using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AopTest
{
    public class UserMamager : IUserMamager
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
            Console.WriteLine("执行：当前方法");
            //模拟出异常
            if (name == "nqicecoffee")
                throw new ArgumentOutOfRangeException("name越界");

            return !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(pass);
        }
    }
}