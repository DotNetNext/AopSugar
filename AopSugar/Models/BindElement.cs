
using System;

namespace AopSugar
{
    /// <summary>
    /// 注册类型的缓存对象（仅程序集内部使用）
    /// </summary>
    internal class BindElement
    {
        public string Name { get; set; }

        public bool IsInstance { get; set; }

        public bool Issingleton { get; set; }

        public Type BindType { get; set; }

        public Type ToType { get; set; }

        public Type AgentType { get; set; }

        public PropertiesAction Assigment { get; set; }
    }
}