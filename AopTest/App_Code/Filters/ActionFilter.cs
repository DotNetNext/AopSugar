using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using AopSugar;

namespace AopTest
{
    /// <summary>
    /// 一般拦截
    /// 功能：打印参数和执行结果，并计算整个方法的执行时间
    /// </summary>
    public class ActionFilter : ActionAttribute
    {
        public override void OnExecuting(AspectContext context)
        {
            Console.WriteLine("执行方法前过滤器");
            //Console.WriteLine("args:");

            //foreach (var item in context.Args)
            //    Console.WriteLine(item);

            ////将计时器加入到当前的上下文环境中
            //Stopwatch sw = new Stopwatch();
            //context.Datas["sw"] = sw;

            //sw.Start();
        }

        public override void OnExecuted(AspectContext context)
        {
            Console.WriteLine("执行方法后过滤器");
            //var sw = context.Datas["sw"] as Stopwatch;
            //if (sw != null)
            //{
            //    //拿到计时器后，计算方法执行的时间
            //    sw.Stop();
            //    Console.WriteLine("takes {0} ms", sw.ElapsedMilliseconds);
            //}

            //Console.WriteLine("\r\nresult is {0}", context.Result);
        }
    }

}