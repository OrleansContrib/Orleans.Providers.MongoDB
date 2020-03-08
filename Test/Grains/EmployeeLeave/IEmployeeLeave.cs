using System;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public interface IEmployeeLeave
    {
        int Identifier { get; set; }

        DateTime DateStart { get; set; }

        DateTime DateEnd { get; set; }
    }
}
