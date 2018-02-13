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
        /// <summary>
        /// 参数值集合
        /// </summary>
        public object[] Args { get; set; }
        /// <summary>
        /// 参数名称集合
        /// </summary>
        public object[] ArgNames { get; set; }
        /// <summary>
        /// 方法名称
        /// </summary>

        public string MethodName { get; set; }
        /// <summary>
        /// 方法详细信息
        /// </summary>
        public MethodInfo MethodInfo { get; set; }
        /// <summary>
        /// 方法AOP属性集合
        /// </summary>
        public object[] Attributes{get;set;}
        /// <summary>
        /// 方法返回值
        /// </summary>

        public object Result { get; set; }
        /// <summary>
        /// 方法方法的类名
        /// </summary>

        public string ClassName { get; set; }
        /// <summary>
        /// 方前方法的命名空间
        /// </summary>
        public string Namespace { get; set; }

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