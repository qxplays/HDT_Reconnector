using System;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace HDT_Reconnector
{
    internal static class Utils
    {
        public static bool IsElevated()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static DateTime ToDateTime(FILETIME time)
        {
            var high = (ulong)time.dwHighDateTime;
            var low = (uint)time.dwLowDateTime;
            var fileTime = (long)((high << 32) + low);
            try
            {
                return DateTime.FromFileTimeUtc(fileTime);
            }
            catch
            {
                return DateTime.FromFileTimeUtc(0xFFFFFFFF);
            }
        }

        public static object GetFieldValue(object obj, string name)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var field = obj.GetType().GetField(name, bindingFlags);
            return field?.GetValue(obj);
        }

        public static void SetFieldValue(object obj, string name, object value)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var field = obj.GetType().GetField(name, bindingFlags);
            field?.SetValue(obj, value);
        }
    }
}
