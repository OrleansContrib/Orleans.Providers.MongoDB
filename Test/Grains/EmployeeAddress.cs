using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class EmployeeAddress
    {
        public string Code { get; set; }
        public string StreetName { get; set; }
        public EmployeeAddressSecret Secret { get; set; }

        public EmployeeAddress()
        {
            this.Code = "0190";
            this.StreetName = "My Street";
            this.Secret = new EmployeeAddressSecret
            {
                No = 1,
                Password = "My Secret"
            };
        }
    }
}
