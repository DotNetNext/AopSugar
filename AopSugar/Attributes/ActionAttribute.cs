using System;

namespace AopSugar
{
    /// <summary>
    /// 一般AOP类别的基类
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public abstract class ActionAttribute : AspectAttribute
    {
        public abstract void OnExecuting(AspectContext context);

        public abstract void OnExecuted(AspectContext context);
    }
}