using System;
using System.Reflection;

namespace HDT_BgPickAdvisor
{
    internal static class Utils
    {
        public static object GetFieldValue(object obj, string name)
        {
            if (obj == null)
                return null;

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var field = obj.GetType().GetField(name, bindingFlags);
            return field?.GetValue(obj);
        }

        public static object GetPropertyValue(object obj, string name)
        {
            if (obj == null)
                return null;

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var prop = obj.GetType().GetProperty(name, bindingFlags);
            return prop?.GetValue(obj);
        }

        public static T GetPropertyValue<T>(object obj, string name)
        {
            var value = GetPropertyValue(obj, name);
            if (value is T typed)
                return typed;
            return default;
        }

        public static bool TryInvoke(object obj, string methodName, object[] args, out object result)
        {
            result = null;
            if (obj == null)
                return false;

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var method = obj.GetType().GetMethod(methodName, bindingFlags);
            if (method == null)
                return false;

            try
            {
                result = method.Invoke(obj, args ?? Array.Empty<object>());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
