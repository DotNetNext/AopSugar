using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AopTest
{
    public class DeptUser : User, IDeptUser
    {
        public string GetDeptName(string name)
        {
            return string.Format("dept_{0}", name);
        }
    }
}