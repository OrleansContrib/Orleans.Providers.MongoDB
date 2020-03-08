using System;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class SickLeave : IEmployeeLeave
    { 
        public int Identifier { get; set; }
        
        public DateTime DateStart { get; set; }

        public DateTime DateEnd { get; set; }
    }
}
