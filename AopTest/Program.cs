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
            var factory = AopContainer.Instance;


            //类
            Console.WriteLine("\r\n***********类使用AOP***********");
            bool flag = factory.CreateInstance<UserMamager>().ValidateUser("张三", "123");

            //接口
            factory.Bind<IUserMamager>().To<UserMamager>();
            var iUser = factory.CreateInstance<IUserMamager>();

            Console.WriteLine("\r\n***********接口使用AOP***********");
            bool flag2 = iUser.ValidateUser("管理员", "admin");

       

            //下面开始异常拦截示例
            Console.WriteLine("\r\n***********异常拦截示例***********");
            flag = iUser.ValidateUser("nqicecoffee", "");

            Console.ReadLine();
        }
    }
}