using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using HDT_Reconnector.Native;
using Hearthstone_Deck_Tracker.Utility.Logging;

namespace HDT_Reconnector.Network
{
    internal class ConnectionBreaker
    {
        private const string HearthstoneProcessName = "Hearthstone";

        public ConnectionStatus Status { get; set; } = ConnectionStatus.Connected;

        public enum ConnectionStatus
        {
            Disconnected,
            Connected
        }

        public int Disconnect(string addr, ushort port)
        {
            var hsTcpRow = GetReconnectTcp(addr, port);
            hsTcpRow.state = (uint)TcpTableNative.MibTcpState.MibTcpStateDeleteTcb;

            var result = TcpTableNative.SetTcpRow(hsTcpRow);
            switch (result)
            {
                case TcpTableNative.SetTcpErrorCode.NoError:
                    Log.Info("Disconnect successfully");
                    Status = ConnectionStatus.Disconnected;
                    return 0;
                case TcpTableNative.SetTcpErrorCode.ErrorAccessDenied:
                    Log.Error("Access denied");
                    break;
                case TcpTableNative.SetTcpErrorCode.ErrorInvalidParameter:
                    Log.Error("Invalid parameter");
                    break;
                case TcpTableNative.SetTcpErrorCode.ErrorNotElevated:
                    Log.Error("Not elevated");
                    break;
                case TcpTableNative.SetTcpErrorCode.ErrorNotSupported:
                    Log.Error("Not supported");
                    break;
                default:
                    Log.Error("Other errors: " + result);
                    break;
            }

            return 1;
        }

        public void MarkConnected()
        {
            Status = ConnectionStatus.Connected;
            Log.Info("Connection restored");
        }

        private static Process[] GetHearthstoneProcesses() => Process.GetProcessesByName(HearthstoneProcessName);

        private static (TcpTableNative.MibTcpRow row, int error) GetHsTcpConnection(
            List<TcpTableNative.MibTcpRowOwnerModule> tcpRows,
            Process[] processes,
            string addr,
            ushort port)
        {
            var pidToName = new Dictionary<uint, string>();
            var tcpRow = new TcpTableNative.MibTcpRow();

            foreach (var process in processes)
                pidToName.Add((uint)process.Id, process.ProcessName);

            for (var i = tcpRows.Count - 1; i >= 0; i--)
            {
                if (!pidToName.ContainsKey(tcpRows[i].ProcessId))
                    continue;

                var remotePort = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(tcpRows[i].remotePort, 0));
                var remoteAddr = new IPAddress(tcpRows[i].remoteAddr).ToString();

                if (!string.IsNullOrEmpty(addr) && port != 0 && remotePort == port && remoteAddr == addr)
                {
                    Log.Info($"Found TCP connection in {pidToName[tcpRows[i].ProcessId]}: {remoteAddr}:{remotePort}");
                    tcpRow.localAddr = tcpRows[i].localAddr;
                    tcpRow.localPort = tcpRows[i].localPort;
                    tcpRow.remoteAddr = tcpRows[i].remoteAddr;
                    tcpRow.remotePort = tcpRows[i].remotePort;
                    return (tcpRow, 0);
                }
            }

            return (tcpRow, 1);
        }

        private static TcpTableNative.MibTcpRow GetReconnectTcp(string addr, ushort port)
        {
            var tcpRows = TcpTableNative.GetAllTcpConnections();
            var hsProcesses = GetHearthstoneProcesses();
            var (hsTcpRow, err) = GetHsTcpConnection(tcpRows, hsProcesses, addr, port);

            if (err == 0)
                return hsTcpRow;

            var lastCreateTimestamp = DateTime.MinValue;
            var pids = new HashSet<uint>();
            foreach (var process in hsProcesses)
                pids.Add((uint)process.Id);

            for (var i = tcpRows.Count - 1; i >= 0; i--)
            {
                if (!pids.Contains(tcpRows[i].ProcessId))
                    continue;

                if (tcpRows[i].CreateTimestamp >= lastCreateTimestamp &&
                    tcpRows[i].State == TcpTableNative.MibTcpState.MibTcpStateEstab)
                {
                    hsTcpRow.localAddr = tcpRows[i].localAddr;
                    hsTcpRow.localPort = tcpRows[i].localPort;
                    hsTcpRow.remoteAddr = tcpRows[i].remoteAddr;
                    hsTcpRow.remotePort = tcpRows[i].remotePort;
                    lastCreateTimestamp = tcpRows[i].CreateTimestamp;
                }
            }

            Log.Info("Using last established Hearthstone TCP connection");
            return hsTcpRow;
        }
    }
}
