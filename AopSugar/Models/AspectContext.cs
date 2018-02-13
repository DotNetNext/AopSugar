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
        /// <summary>
        /// 参数值集合
        /// </summary>
        public object[] Args { get; set; }
        /// <summary>
        /// 参数名称集合
        /// </summary>
        public string[] ArgNames { get; set; }
        /// <summary>
        /// 参数明细
        /// </summary>
        public Dictionary<string, object> ArgDetails
        {
            get
            {
                if (_ArgDetails == null)
                {
                    _ArgDetails = new Dictionary<string, object>();
                    if (Args != null && ArgNames != null && Args.Length == ArgNames.Length) {
                        for (int i = 0; i < Args.Length; i++)
                        {
                            _ArgDetails.Add(ArgNames[i],Args[i]);
                        }
                    }
                }

                return _ArgDetails;
            }
        }
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

        private Dictionary<string, object> _ArgDetails = null;
    }
}