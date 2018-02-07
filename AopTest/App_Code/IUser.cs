using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AopTest
{
    public interface IUser
    {
        string Name { get; set; }

        bool ValidateUser(string name, string pass);

        DateTime Birthday { get; set; }

        string Version { get; set; }
    }
}