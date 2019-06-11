using System;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public interface IEmployeeLeave
    {
        int Identifier { get; set; }

        DateTime StartDate { get; set; }

        DateTime EndDate { get; set; }
    }
}
