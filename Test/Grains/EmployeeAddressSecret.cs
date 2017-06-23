using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class EmployeeAddressSecret
    {
        public int No { get; set; }
        public string Password { get; set; }
    }
}
