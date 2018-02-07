using System;

namespace DotNet.EasyAssemble
{
    /// <summary>
    /// 一般AOP类别的基类
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public abstract class BasicAttribute : AspectAttribute
    {
        public abstract void OnExecuting(AspectContext context);

        public abstract void OnExecuted(AspectContext context);
    }
}