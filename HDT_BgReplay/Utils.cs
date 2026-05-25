using System;
using System.Reflection;

namespace HDT_BgReplay
{
    internal static class Utils
    {
        public static object GetFieldValue(object obj, string name)
        {
            if (obj == null)
                return null;

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            return obj.GetType().GetField(name, flags)?.GetValue(obj);
        }
    }
}
