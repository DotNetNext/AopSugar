using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AopSugar
{
    internal class MethodHelper
    {
        public static bool IsObjectMethod(MethodInfo method)
        {
            string[] arr = new string[] { "ToString", "GetType", "GetHashCode", "Equals" };
            return arr.Contains(method.Name);
        }
        public static Type[] GetParameterTypes(MethodInfo method)
        {
            var paramInfos = method.GetParameters();
            int len = paramInfos.Length;
            Type[] paramTypes = new Type[len];
            for (int i = 0; i < len; i++)
                paramTypes[i] = paramInfos[i].ParameterType;

            return paramTypes;
        }


    }
}
