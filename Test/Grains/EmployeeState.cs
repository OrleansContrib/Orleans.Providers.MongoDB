using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class EmployeeState
    {
        public int Level { get; set; }

        public EmployeeAddress Address { get; set; }

        public string[] FavouriteColours { get; set; }

        public EmployeeState()
        {
            //this.Address = new EmployeeAddress();
            this.FavouriteColours = new string[] { "Red", "Blue", "Green" };
        }
    }
}
