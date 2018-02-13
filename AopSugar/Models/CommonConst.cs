using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AopSugar
{
    public class CommonConst
    {
        public static Type I1 = typeof(Byte);
        public static Type I2 = typeof(Int16);
        public static Type I4 = typeof(Int32);
        public static Type I8 = typeof(Int64);
        public static Type R4 = typeof(float);
        public static Type U1 = typeof(uint);
        public static Type U2 = typeof(UInt16);
        public static Type U4 = typeof(UInt32);
        public static Type S0 = typeof(string);

        public static Type BT = typeof(ActionAttribute);
        public static Type ET = typeof(ExceptionAttribute);
        public static Type AT = typeof(AuthenticationAttribute);

        public static AssembleFactory m_Instance = null;
        public static object m_InstanceLocker = new object();
    }
}
