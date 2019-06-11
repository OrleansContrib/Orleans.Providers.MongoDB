using System;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class SickLeave : IEmployeeLeave
    { 
        public int Identifier { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
