using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AopSugar
{
    public class MethodHelper
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

        public static MethodInfo GetMethod(object obj, string name,object [] args)
        {
            var methods= obj.GetType().GetMethods().Where(it=>it.Name==name&&it.IsVirtual==true);
            if (methods.Count() == 1) return methods.First();
            MethodInfo result = null;
            foreach (var item in methods)
            {
                var itemArgsTypes =string.Join(",", item.GetParameters().Select(it=>it.ParameterType.Name).ToArray());
                var argsTypes = string.Join(",", args.Select(it => it.GetType().Name).ToArray());
                if (argsTypes == itemArgsTypes) {
                    result = item;
                    break; 
                }
            }
            return result;
        }

        public static object[] GetCustomAttributes(object obj, string name, object[] args)
        {
            return GetMethod(obj,name,args).GetCustomAttributes(true);
        }

        public static ParameterInfo[] GetParameterNames(object obj, string name, object[] args) {

            return GetMethod(obj, name, args).GetParameters();
        }
    }
}
