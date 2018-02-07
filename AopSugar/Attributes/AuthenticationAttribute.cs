using System;

namespace AopSugar
{
    /// <summary>
    /// 认证类AOP的抽象基类
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public abstract class AuthenticationAttribute : AspectAttribute
    {
        public abstract bool OnAuthentication(AspectContext context);
    }
}