using AopSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AopTest
{
    public class TestModel
    {
        public IUserMamager User { get; set; }

        public IUserMamager AdminUser { get; set; }

        public IDeptUser DeptUser { get; set; }

        public ISingleton Singleton { get; set; }

        public string Version { get; set; }
    }
}