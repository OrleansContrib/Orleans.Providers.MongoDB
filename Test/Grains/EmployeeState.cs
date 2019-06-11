using System.Collections.Generic;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public sealed class EmployeeState
    {
        public int Level { get; set; }

        public EmployeeAddress Address { get; set; } = new EmployeeAddress();

        public string[] FavouriteColours { get; set; } = new[] 
            {
                "Red",
                "Blue",
                "Green"
            };

        public List<int> RandomNumbersOwned { get; set; } = new List<int>
            {
                1,
                2,
                3
            };

        public EmployeeStatus Status { get; set; }

        public IList<IEmployeeLeave> EmployeeLeave { get; set; } = new List<IEmployeeLeave>();
    }
}