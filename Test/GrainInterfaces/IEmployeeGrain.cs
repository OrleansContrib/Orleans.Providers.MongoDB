namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface IEmployeeGrain : IGrainWithIntegerKey
    {
        Task SetLevel(int level);

        Task<int> ReturnLevel();
    }
}