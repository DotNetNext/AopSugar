using AopSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AopTest
{

    /// <summary>
    /// 一般拦截
    /// 功能：打印参数和执行结果，并计算整个方法的执行时间
    /// </summary>
    public class LogFilter : ActionAttribute
    {
        public override void OnExecuting(AspectContext context)
        {

        }

        public override void OnExecuted(AspectContext context)
        {
            Console.WriteLine("执行：日志记录");
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
