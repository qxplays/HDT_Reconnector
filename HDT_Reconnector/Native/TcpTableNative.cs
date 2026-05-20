using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace HDT_Reconnector.Native
{
    internal static class TcpTableNative
    {
        public const int AfInet = 2;

        public enum MibTcpState
        {
            MibTcpStateClosed = 1,
            MibTcpStateListen = 2,
            MibTcpStateSynSent = 3,
            MibTcpStateSynRcvd = 4,
            MibTcpStateEstab = 5,
            MibTcpStateFinWait1 = 6,
            MibTcpStateFinWait2 = 7,
            MibTcpStateCloseWait = 8,
            MibTcpStateClosing = 9,
            MibTcpStateLastAck = 10,
            MibTcpStateTimeWait = 11,
            MibTcpStateDeleteTcb = 12
        }

        private enum TcpTableClass
        {
            TcpTableOwnerModuleAll = 8
        }

        public enum SetTcpErrorCode
        {
            NoError = 0,
            ErrorAccessDenied = 5,
            ErrorNotSupported = 50,
            ErrorInvalidParameter = 87,
            ErrorNotElevated = 317
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MibTcpRow
        {
            public uint state;
            public uint localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] localPort;
            public uint remoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] remotePort;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MibTcpRowOwnerModule
        {
            public uint state;
            public uint localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] localPort;
            public uint remoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] remotePort;
            public uint owningPid;
            public FILETIME liCreateTimestamp;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public ulong[] owningModuleInfo;

            public uint ProcessId => owningPid;

            public MibTcpState State => (MibTcpState)state;

            public DateTime CreateTimestamp => Utils.ToDateTime(liCreateTimestamp);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcpTableOwnerModule
        {
            public uint dwNumEntries;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 1)]
            public MibTcpRowOwnerModule[] table;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate uint GetExtendedTcpTableDelegate(
            IntPtr pTcpTable,
            ref int dwOutBufLen,
            bool sort,
            int ipVersion,
            TcpTableClass tblClass,
            uint reserved);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int SetTcpEntryDelegate(IntPtr pTcprow);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        private static readonly Lazy<GetExtendedTcpTableDelegate> GetExtendedTcpTableFunc = new Lazy<GetExtendedTcpTableDelegate>(LoadGetExtendedTcpTable);
        private static readonly Lazy<SetTcpEntryDelegate> SetTcpEntryFunc = new Lazy<SetTcpEntryDelegate>(LoadSetTcpEntry);

        private static string NetworkHelperLibraryName =>
            new string(new[] { 'i', 'p', 'h', 'l', 'p', 'a', 'p', 'i', '.', 'd', 'l', 'l' });

        private static IntPtr LoadNetworkHelper()
        {
            var handle = LoadLibrary(NetworkHelperLibraryName);
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("Failed to load network helper library.");
            return handle;
        }

        private static GetExtendedTcpTableDelegate LoadGetExtendedTcpTable()
        {
            var lib = LoadNetworkHelper();
            var ptr = GetProcAddress(lib, "GetExtendedTcpTable");
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException("GetExtendedTcpTable not found.");
            return Marshal.GetDelegateForFunctionPointer<GetExtendedTcpTableDelegate>(ptr);
        }

        private static SetTcpEntryDelegate LoadSetTcpEntry()
        {
            var lib = LoadNetworkHelper();
            var ptr = GetProcAddress(lib, "SetTcpEntry");
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException("SetTcpEntry not found.");
            return Marshal.GetDelegateForFunctionPointer<SetTcpEntryDelegate>(ptr);
        }

        public static List<MibTcpRowOwnerModule> GetAllTcpConnections()
        {
            return GetTcpConnections<MibTcpRowOwnerModule, MibTcpTableOwnerModule>(AfInet);
        }

        public static SetTcpErrorCode SetTcpRow(MibTcpRow row)
        {
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MibTcpRow)));
            try
            {
                Marshal.StructureToPtr(row, ptr, false);
                return (SetTcpErrorCode)SetTcpEntryFunc.Value(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private static List<TcpRow> GetTcpConnections<TcpRow, TcpTable>(int ipVersion)
        {
            var rowStructSize = Marshal.SizeOf(typeof(TcpRow));
            var buffSize = 0;
            GetExtendedTcpTableFunc.Value(IntPtr.Zero, ref buffSize, false, ipVersion, TcpTableClass.TcpTableOwnerModuleAll, 0);

            var tcpTablePtr = Marshal.AllocHGlobal(buffSize);
            try
            {
                var ret = GetExtendedTcpTableFunc.Value(tcpTablePtr, ref buffSize, false, ipVersion, TcpTableClass.TcpTableOwnerModuleAll, 0);
                if (ret != 0)
                    return new List<TcpRow>();

                var table = (TcpTable)Marshal.PtrToStructure(tcpTablePtr, typeof(TcpTable));
                var numEntriesField = typeof(TcpTable).GetField("dwNumEntries");
                var numEntries = (uint)numEntriesField.GetValue(table);
                var rows = new TcpRow[numEntries];
                var rowPtr = (IntPtr)((long)tcpTablePtr + Marshal.OffsetOf(typeof(TcpTable), "table").ToInt64());

                for (var i = 0; i < numEntries; i++)
                {
                    rows[i] = (TcpRow)Marshal.PtrToStructure(rowPtr, typeof(TcpRow));
                    rowPtr = (IntPtr)((long)rowPtr + rowStructSize);
                }

                return rows.ToList();
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTablePtr);
            }
        }
    }
}
