using System;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class VacationLeave : IEmployeeLeave
    {
        public int Identifier { get; set; }
        
        public DateTime DateStart { get; set; }

        public DateTime DateEnd { get; set; }
    }
}
