namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class EmployeeAddress
    {
        public EmployeeAddress()
        {
            Code = "0190";
            StreetName = "My Street";
            Secret = new EmployeeAddressSecret
            {
                No = 1,
                Password = "My Secret"
            };
        }

        public string Code { get; set; }
        public string StreetName { get; set; }
        public EmployeeAddressSecret Secret { get; set; }
    }
}