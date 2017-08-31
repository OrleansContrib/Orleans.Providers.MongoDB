using System.Collections.Generic;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class EmployeeState
    {
        public EmployeeState()
        {
            Address = new EmployeeAddress();
            FavouriteColours = new[] {"Red", "Blue", "Green"};
            RandomNumbersOwned = new List<int>();
            RandomNumbersOwned.Add(1);
            RandomNumbersOwned.Add(2);
            RandomNumbersOwned.Add(3);
            Status = EmployeeStatus.NotActive;
        }

        public int Level { get; set; }

        public EmployeeAddress Address { get; set; }

        public string[] FavouriteColours { get; set; }

        public List<int> RandomNumbersOwned { get; set; }

        public EmployeeStatus Status { get; set; }
    }
}