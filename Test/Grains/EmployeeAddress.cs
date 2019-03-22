namespace Orleans.Providers.MongoDB.Test.Grains
{
    public sealed class EmployeeAddress
    {
        public string Code { get; set; } = "0190";

        public string StreetName { get; set; } = "My Street";

        public EmployeeAddressSecret Secret { get; set; } = new EmployeeAddressSecret { No = 1, Password = "Password" };
    }
}