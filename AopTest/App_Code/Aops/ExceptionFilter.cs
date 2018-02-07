using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using AopSugar;

namespace AopTest
{
    /// <summary>
    /// 异常拦截
    /// 功能：处理方法执行的所有异常
    /// </summary>
    public class ExceptionFilter : ExceptionAttribute
    {
        public override void OnException(AspectContext context, Exception ex)
        {
            Console.WriteLine("执行：异常过滤器");
        }
    }
}