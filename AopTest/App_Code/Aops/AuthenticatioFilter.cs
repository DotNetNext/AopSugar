using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using AopSugar;

namespace AopTest
{
    /// <summary>
    /// 认证拦截
    /// 功能：校验每个参数是否为null，如果为null，则认证不通过（真正的方法不会执行）
    ///       模拟认证过程（不必较真演示的场景）
    /// </summary>
    public class AuthenticatioFilter : AuthenticationAttribute
    {
        public override bool OnAuthentication(AspectContext context)
        {
            foreach (var item in context.Args)
            {
                if (item == null)
                    return false;
            }
            Console.WriteLine("执行：验证过滤器");
            return true;
        }
    }
}