using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AopSugar
{
    public class TypeHelper
    {
        public static Type GetUnRefType(Type type)
        {
            return Type.GetType(string.Format("{0}, {1}", type.FullName.TrimEnd('&'), type.Assembly.FullName), true, true);
        }
    }
}
