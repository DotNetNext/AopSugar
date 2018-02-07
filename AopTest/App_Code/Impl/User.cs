using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AopTest
{
    public class User : IUser
    {

        public string Version { get; set; }

        public string Name { get; set; }

  
        public DateTime Birthday { get; set; }

        [ActionFilter]
        [AuthenticatioFilter]
        [ExceptionFilter]
        [LogFilter]
        public virtual bool ValidateUser(string name, string pass)
        {
            //模拟方法的耗时
            Random rnd = new Random();
            System.Threading.Thread.Sleep(rnd.Next(100, 1000));

            //模拟出异常
            if (name == "nqicecoffee")
                throw new ArgumentOutOfRangeException("name越界");

            return !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(pass);
        }
    }
}