using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AopTest
{
    public interface IDeptUser : IUser
    {
        string GetDeptName(string name);
    }
}