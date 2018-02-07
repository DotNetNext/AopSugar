using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using AopSugar;

namespace AopTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = AssembleFactory.Instance;
 
            factory.Bind<IUserMamager>().To<UserMamager>();
            var user = factory.CreateInstance<UserMamager>();
            var iUser = factory.CreateInstance<IUserMamager>();


            //下面开始一般拦截示例
            Console.WriteLine("\r\n***********类AOP***********");
            bool flag = user.ValidateUser("张三", "123");

            Console.WriteLine("\r\n***********接口AOP***********");
            bool flag2 = iUser.ValidateUser("管理员", "admin");

       

            //下面开始异常拦截示例
            Console.WriteLine("\r\n***********异常拦截示例***********");
            flag = iUser.ValidateUser("nqicecoffee", "");

            Console.ReadLine();
        }
    }
}