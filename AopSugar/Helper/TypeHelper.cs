using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AopSugar
{
    internal class TypeHelper
    {
        public static Type GetUnRefType(Type type)
        {
            return Type.GetType(string.Format("{0}, {1}", type.FullName.TrimEnd('&'), type.Assembly.FullName), true, true);
        }
        public static MethodInfo[] GetAllMethods(IList<Type> list)
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (var item in list)
                methods.AddRange(item.GetMethods(BindingFlags.Public | BindingFlags.Instance));

            return methods.ToArray();
        }

        public static PropertyInfo[] GetAllPropeties(IList<Type> list)
        {
            List<PropertyInfo> props = new List<PropertyInfo>();
            foreach (var item in list)
                props.AddRange(item.GetProperties(BindingFlags.Public | BindingFlags.Instance));

            return props.ToArray();
        }

        public static void GetAllFatherInterfaces(Type type, IList<Type> list)
        {
            list.Add(type);
            var types = type.GetInterfaces(); //获取所有的继承的接口

            if (types == null || types.Length == 0)
                return;

            foreach (var item in types)
                GetAllFatherInterfaces(item, list);
        }

    }
}
