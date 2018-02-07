using System;
using System.Collections.Generic;

namespace AopSugar
{
    /// <summary>
    /// 能够装配的属性的缓存类（仅程序集内部使用）
    /// </summary>
    internal class PropertiesAction
    {
        public IList<BindElement> RegisterTypes { get; set; }

        public Action<object, object[]> Action { get; set; }
    }
}