using System;
using Microsoft.Extensions.Logging;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;

namespace Orleans.Providers.MongoDB.Test.Client
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var client = new ClientBuilder()
                .ConfigureApplicationParts(options =>
                {
                    options.AddApplicationPart(typeof(IHelloWorldGrain).Assembly);
                })
                .UseMongoDBGatewayListProvider(options =>
                {
                    options.ConnectionString = "mongodb://localhost/OrleansTestApp";
                })
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            client.Connect().Wait();

            // get a reference to the grain from the grain factory
            var helloWorldGrain = client.GetGrain<IHelloWorldGrain>(1);

            // call the grain
            helloWorldGrain.SayHello("World").Wait();

            var reminderGrain = client.GetGrain<INewsReminderGrain>(1);

            reminderGrain.StartReminder("TestReminder", TimeSpan.FromMinutes(10)).Wait();
            reminderGrain.RemoveReminder("TestReminder");

            // Test State 

            var employee = client.GetGrain<IEmployeeGrain>(1);
            var employeeId = employee.ReturnLevel().Result;

            if (employeeId == 100)
            {
                employee.SetLevel(50);
            }
            else
            {
                employee.SetLevel(100);
            }

            employeeId = employee.ReturnLevel().Result;
            
            Console.WriteLine(employeeId);
            Console.ReadKey();
        }
    }
}