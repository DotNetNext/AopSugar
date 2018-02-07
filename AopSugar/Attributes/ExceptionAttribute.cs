using System;

namespace AopSugar
{
    /// <summary>
    /// 异常AOP的抽象基类
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public abstract class ExceptionAttribute : AspectAttribute
    {
        public abstract void OnException(AspectContext context, Exception ex);
    }
}