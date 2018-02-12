using System;
using System.Collections.Generic;
using System.Reflection;

namespace AopSugar
{
    /// <summary>
    /// AOP参数中的上下文对象
    /// </summary>
    public class AspectContext
    {
        private Dictionary<string, object> m_Datas = null;

        public object[] Args { get; set; }

        public object Result { get; set; }

        public string MethodName { get; set; }

        public Dictionary<string, object> Datas
        {
            get
            {
                if (m_Datas == null)
                    m_Datas = new Dictionary<string, object>();

                return m_Datas;
            }
        }
    }
}