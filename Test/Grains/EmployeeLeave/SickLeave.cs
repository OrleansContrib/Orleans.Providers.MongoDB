using System;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class SickLeave : IEmployeeLeave
    {
        public SickLeave(int identifier)
        {
            Identifier = identifier;
        }

        public int Identifier { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now.Date;
        public DateTime EndDate { get; set; } = DateTime.Now.Date;
        public int AmountInHours { get; set; } = 8;
    }
}
