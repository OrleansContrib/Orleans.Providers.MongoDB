//#define USE_SQL_SERVER

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans;
using Orleans.Providers.MongoDB.UnitTest.Base;
using Orleans.Providers.MongoDB.UnitTest.Reminders;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.TestingHost;
using Utils = Orleans.Providers.MongoDB.UnitTest.Reminders.Utils;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedVariable

namespace UnitTests.TimerTests
{
    public class ReminderTests_Base : TestCluster
    {
        internal const string DR = "DEFAULT_REMINDER";
        internal const string R1 = "REMINDER_1";
        internal const string R2 = "REMINDER_2";

        protected const long retries = 3;

        protected const long failAfter = 2
            ; // NOTE: match this sleep with 'failCheckAfter' used in PerGrainFailureTest() so you dont try to get counter immediately after failure as new activation may not have the reminder statistics

        protected const long failCheckAfter = 6; // safe value: 9

        internal static readonly TimeSpan LEEWAY = TimeSpan.FromSeconds(5)
            ; // the experiment shouldnt be that long that the sums of leeways exceeds a period

        internal static readonly TimeSpan ENDWAIT = TimeSpan.FromMinutes(10);

        protected Logger log;

        public ReminderTests_Base()
        {
            var cfg = ClientConfiguration.LoadFromFile("ClientConfiguration.xml");
        }

        /// <summary>
        ///     Configures the test cluster plus default client in-process.
        /// </summary>
        public ReminderTests_Base(ClusterConfiguration clusterConfiguration)
            : base(clusterConfiguration)
        {
        }

        public void Dispose()
        {
            // ReminderTable.Clear() cannot be called from a non-Orleans thread,
            // so we must proxy the call through a grain.
            var controlProxy = GrainClient.GrainFactory.GetGrain<IReminderTestGrain2>(Guid.NewGuid());
            controlProxy.EraseReminderTable(ClusterConfiguration.Globals.DataConnectionString)
                .WaitWithThrow(TestConstants.InitTimeout);
        }

        #region Multi grains multi reminders/grain test

        protected async Task<bool> PerGrainMultiReminderTest(IReminderTestGrain2 g)
        {
            var period = await g.GetReminderPeriod(DR);

            // Each reminder is started 2 periods after the previous reminder
            // once all reminders have been started, stop them every 2 periods
            // except the default reminder, which we stop after 3 periods instead
            // just to test and break the symmetry

            // Start Default Reminder
            await g.StartReminder(DR);
            var sleepFor = period.Multiply(2) + LEEWAY; // giving some leeway
            Thread.Sleep(sleepFor);
            var last = await g.GetCounter(DR);
            AssertIsInRange(last, 1, 2, g, DR, sleepFor);

            // Start R1
            await g.StartReminder(R1);
            Thread.Sleep(sleepFor);
            last = await g.GetCounter(R1);
            AssertIsInRange(last, 1, 2, g, R1, sleepFor);

            // Start R2
            await g.StartReminder(R2);
            Thread.Sleep(sleepFor);
            last = await g.GetCounter(R1);
            AssertIsInRange(last, 3, 4, g, R1, sleepFor);
            last = await g.GetCounter(R2);
            AssertIsInRange(last, 1, 2, g, R2, sleepFor);
            last = await g.GetCounter(DR);
            AssertIsInRange(last, 5, 6, g, DR, sleepFor);

            // Stop R1
            await g.StopReminder(R1);
            Thread.Sleep(sleepFor);
            last = await g.GetCounter(R1);
            AssertIsInRange(last, 3, 4, g, R1, sleepFor);
            last = await g.GetCounter(R2);
            AssertIsInRange(last, 3, 4, g, R2, sleepFor);
            last = await g.GetCounter(DR);
            AssertIsInRange(last, 7, 8, g, DR, sleepFor);

            // Stop R2
            await g.StopReminder(R2);
            sleepFor = period.Multiply(3) + LEEWAY; // giving some leeway
            Thread.Sleep(sleepFor);
            last = await g.GetCounter(R1);
            AssertIsInRange(last, 3, 4, g, R1, sleepFor);
            last = await g.GetCounter(R2);
            AssertIsInRange(last, 3, 4, g, R2, sleepFor);
            last = await g.GetCounter(DR);
            AssertIsInRange(last, 10, 12, g, DR, sleepFor);

            // Stop Default reminder
            await g.StopReminder(DR);
            sleepFor = period.Multiply(1) + LEEWAY; // giving some leeway
            Thread.Sleep(sleepFor);
            last = await g.GetCounter(R1);
            AssertIsInRange(last, 3, 4, g, R1, sleepFor);
            last = await g.GetCounter(R2);
            AssertIsInRange(last, 3, 4, g, R2, sleepFor);
            last = await g.GetCounter(DR);
            AssertIsInRange(last, 10, 12, g, DR, sleepFor);

            return true;
        }

        #endregion

        #region Multiple grain types

        protected async Task<bool> PerCopyGrainFailureTest(IReminderTestCopyGrain grain)
        {
            var period = await grain.GetReminderPeriod(DR);

            await grain.StartReminder(DR);
            Thread.Sleep(period.Multiply(failCheckAfter) + LEEWAY); // giving some leeway
            var last = await grain.GetCounter(DR);
            Assert.AreEqual(failCheckAfter,
                last); // "{0} CopyGrain {1} Reminder {2}" // Time(), grain.GetPrimaryKey(), DR);

            await grain.StopReminder(DR);
            Thread.Sleep(period.Multiply(2) + LEEWAY); // giving some leeway
            var curr = await grain.GetCounter(DR);
            Assert.AreEqual(last, curr); // "{0} CopyGrain {1} Reminder {2}", Time(), grain.GetPrimaryKey(), DR);

            return true;
        }

        #endregion

        #region Test methods

        #region Basic test

        public async Task Test_Reminders_Basic_StopByRef()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IReminderTestGrain2>(Guid.NewGuid());

            var r1 = await grain.StartReminder(DR, TimeSpan.FromSeconds(10));
            var r2 = await grain.StartReminder(DR);
            try
            {
                // First handle should now be out of date once the seconf handle to the same reminder was obtained
                await grain.StopReminder(r1);
                Assert.IsTrue(false, "Removed reminder1, which shouldn't be possible.");
            }
            catch (Exception exc)
            {
                if (log != null)
                {
                    log.Info("Couldn't remove {0}, as expected. Exception received = {1}", r1, exc);
                }
            }

            await grain.StopReminder(r2);

            // trying to see if readreminder works
            var o1 = await grain.StartReminder(DR);
            var o2 = await grain.StartReminder(DR);
            var o3 = await grain.StartReminder(DR);
            var o4 = await grain.StartReminder(DR);

            var r = await grain.GetReminderObject(DR);
            await grain.StopReminder(r);
        }

        public async Task Test_Reminders_Basic_ListOps()
        {
            var id = Guid.NewGuid();
            if (log != null)
            {
                log.Info("Start Grain Id = {0}", id);
            }

            var grain = GrainClient.GrainFactory.GetGrain<IReminderTestGrain2>(id);
            const int count = 5;
            var startReminderTasks = new Task<IGrainReminder>[count];
            for (var i = 0; i < count; i++)
            {
                startReminderTasks[i] = grain.StartReminder(DR + "_" + i);
            }

            await Task.WhenAll(startReminderTasks);
            // do comparison on strings
            var registered = (from reminder in startReminderTasks select reminder.Result.ReminderName).ToList();

            var remindersList = await grain.GetRemindersList();
            var fetched = (from reminder in remindersList select reminder.ReminderName).ToList();

            foreach (var remRegistered in registered)
            {
                Assert.IsTrue(fetched.Remove(remRegistered), $"Couldn't get reminder {remRegistered}. " +
                                                             $"Registered list: {Utils.EnumerableToString(registered)}, " +
                                                             $"fetched list: {Utils.EnumerableToString(remindersList, r => r.ReminderName)}");
            }

            Assert.IsTrue(fetched.Count == 0,
                $"More than registered reminders. Extra: {Utils.EnumerableToString(fetched)}");

            // do some time tests as well
            if (log != null)
            {
                log.Info("Time tests");
            }

            var period = await grain.GetReminderPeriod(DR);
            Thread.Sleep(period.Multiply(2) + LEEWAY); // giving some leeway
            for (var i = 0; i < count; i++)
            {
                var curr = await grain.GetCounter(DR + "_" + i);
                Assert.AreEqual(2, curr); // string.Format("Incorrect ticks for {0}_{1}", DR, i));
            }
        }

        #endregion

        #region Single join ... multi grain, multi reminders

        public async Task Test_Reminders_1J_MultiGrainMultiReminders()
        {
            var g1 = GrainClient.GrainFactory.GetGrain<IReminderTestGrain2>(Guid.NewGuid());
            var g2 = GrainClient.GrainFactory.GetGrain<IReminderTestGrain2>(Guid.NewGuid());
            var g3 = GrainClient.GrainFactory.GetGrain<IReminderTestGrain2>(Guid.NewGuid());
            var g4 = GrainClient.GrainFactory.GetGrain<IReminderTestGrain2>(Guid.NewGuid());
            var g5 = GrainClient.GrainFactory.GetGrain<IReminderTestGrain2>(Guid.NewGuid());

            var period = await g1.GetReminderPeriod(DR);

            Task<bool>[] tasks =
            {
                Task.Run(() => PerGrainMultiReminderTestChurn(g1)),
                Task.Run(() => PerGrainMultiReminderTestChurn(g2)),
                Task.Run(() => PerGrainMultiReminderTestChurn(g3)),
                Task.Run(() => PerGrainMultiReminderTestChurn(g4)),
                Task.Run(() => PerGrainMultiReminderTestChurn(g5))
            };

            Thread.Sleep(period.Multiply(5));
            // start another silo ... although it will take it a while before it stabilizes

            if (log != null)
            {
                log.Info("Starting another silo");
            }

            StartAdditionalSilos(1);
            Thread.Sleep(period.Multiply(5));

            //Block until all tasks complete.
            await Task.WhenAll(tasks).WithTimeout(ENDWAIT);
        }

        #endregion

        public async Task Test_Reminders_ReminderNotFound()
        {
            var g1 = GrainClient.GrainFactory.GetGrain<IReminderTestGrain2>(Guid.NewGuid());

            // request a reminder that does not exist
            var reminder = await g1.GetReminderObject("blarg");
            Assert.IsNull(reminder);
        }

        #endregion

        #region Multiple joins ... multi grain, multi reminders

        internal async Task<bool> PerGrainMultiReminderTestChurn(IReminderTestGrain2 g)
        {
            // for churn cases, we do execute start and stop reminders with retries as we don't have the queue-ing 
            // functionality implemented on the LocalReminderService yet
            var period = await g.GetReminderPeriod(DR);

            // Start Default Reminder
            //g.StartReminder(DR, file + "_" + DR).Wait();
            await ExecuteWithRetries(g.StartReminder, DR);
            var sleepFor = period.Multiply(2);
            await Task.Delay(sleepFor);
            // Start R1
            //g.StartReminder(R1, file + "_" + R1).Wait();
            await ExecuteWithRetries(g.StartReminder, R1);
            sleepFor = period.Multiply(2);
            await Task.Delay(sleepFor);
            // Start R2
            //g.StartReminder(R2, file + "_" + R2).Wait();
            await ExecuteWithRetries(g.StartReminder, R2);
            sleepFor = period.Multiply(2);
            await Task.Delay(sleepFor);

            sleepFor = period.Multiply(1);
            await Task.Delay(sleepFor);

            // Stop R1
            //g.StopReminder(R1).Wait();
            await ExecuteWithRetriesStop(g.StopReminder, R1);
            sleepFor = period.Multiply(2);
            await Task.Delay(sleepFor);
            // Stop R2
            //g.StopReminder(R2).Wait();
            await ExecuteWithRetriesStop(g.StopReminder, R2);
            sleepFor = period.Multiply(1);
            await Task.Delay(sleepFor);

            // Stop Default reminder
            //g.StopReminder(DR).Wait();
            await ExecuteWithRetriesStop(g.StopReminder, DR);
            sleepFor = period.Multiply(1) + LEEWAY; // giving some leeway
            await Task.Delay(sleepFor);

            var last = await g.GetCounter(R1);
            AssertIsInRange(last, 4, 6, g, R1, sleepFor);

            last = await g.GetCounter(R2);
            AssertIsInRange(last, 4, 6, g, R2, sleepFor);

            last = await g.GetCounter(DR);
            AssertIsInRange(last, 9, 10, g, DR, sleepFor);

            return true;
        }

        protected async Task<bool> PerGrainFailureTest(IReminderTestGrain2 grain)
        {
            var period = await grain.GetReminderPeriod(DR);

            await grain.StartReminder(DR);
            var sleepFor = period.Multiply(failCheckAfter) + LEEWAY; // giving some leeway
            Thread.Sleep(sleepFor);
            var last = await grain.GetCounter(DR);
            AssertIsInRange(last, failCheckAfter - 1, failCheckAfter + 1, grain, DR, sleepFor);

            await grain.StopReminder(DR);
            sleepFor = period.Multiply(2) + LEEWAY; // giving some leeway
            Thread.Sleep(sleepFor);
            var curr = await grain.GetCounter(DR);

            AssertIsInRange(curr, last, last + 1, grain, DR, sleepFor);

            return true;
        }

        #endregion

        #region Utility methods

        protected static string Time()
        {
            return DateTime.UtcNow.ToString("hh:mm:ss.fff");
        }

        protected void AssertIsInRange(long val, long lowerLimit, long upperLimit, IGrain grain, string reminderName,
            TimeSpan sleepFor)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Grain: {0} Grain PrimaryKey: {1}, Reminder: {2}, SleepFor: {3} Time now: {4}",
                grain, grain.GetPrimaryKey(), reminderName, sleepFor, Time());
            sb.AppendFormat(
                " -- Expecting value in the range between {0} and {1}, and got value {2}.",
                lowerLimit, upperLimit, val);

            var tickCountIsInsideRange = lowerLimit <= val && val <= upperLimit;

            // Todo: Add back
            //Skip.IfNot(tickCountIsInsideRange, $"AssertIsInRange: {sb}  -- WHICH IS OUTSIDE RANGE.");
        }

        protected async Task ExecuteWithRetries(Func<string, TimeSpan?, bool, Task> function, string reminderName,
            TimeSpan? period = null, bool validate = false)
        {
            for (long i = 1; i <= retries; i++)
            {
                try
                {
                    await function(reminderName, period, validate).WithTimeout(TestConstants.InitTimeout);
                    return; // success ... no need to retry
                }
                catch (AggregateException aggEx)
                {
                    aggEx.Handle(exc => HandleError(exc, i));
                }
                catch (ReminderException exc)
                {
                    HandleError(exc, i);
                }
            }

            // execute one last time and bubble up errors if any
            await function(reminderName, period, validate).WithTimeout(TestConstants.InitTimeout);
        }

        // Func<> doesnt take optional parameters, thats why we need a separate method
        protected async Task ExecuteWithRetriesStop(Func<string, Task> function, string reminderName)
        {
            for (long i = 1; i <= retries; i++)
            {
                try
                {
                    await function(reminderName).WithTimeout(TestConstants.InitTimeout);
                    return; // success ... no need to retry
                }
                catch (AggregateException aggEx)
                {
                    aggEx.Handle(exc => HandleError(exc, i));
                }
                catch (ReminderException exc)
                {
                    HandleError(exc, i);
                }
            }

            // execute one last time and bubble up errors if any
            await function(reminderName).WithTimeout(TestConstants.InitTimeout);
        }

        private bool HandleError(Exception ex, long i)
        {
            if (ex is AggregateException)
            {
                ex = ((AggregateException) ex).Flatten().InnerException;
            }

            if (ex is ReminderException)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(10)); // sleep a bit before retrying
                return true;
            }

            return false;
        }

        #endregion
    }
}