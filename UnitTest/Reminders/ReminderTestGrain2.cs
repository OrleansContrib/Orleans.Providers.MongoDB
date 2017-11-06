using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans;
using Orleans.Providers.MongoDB.Reminders.Repository;
using Orleans.Providers.MongoDB.UnitTest.Reminders;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Utils = Orleans.Providers.MongoDB.UnitTest.Reminders.Utils;

#pragma warning disable 612,618

namespace UnitTests.Grains
{
    // NOTE: if you make any changes here, copy them to ReminderTestCopyGrain
    public class ReminderTestGrain2 : Grain, IReminderTestGrain2, IRemindable
    {
        private static readonly long ACCURACY = 50 * TimeSpan.TicksPerMillisecond
            ; // when we use ticks to compute sequence numbers, we might get wrong results as timeouts don't happen with precision of ticks  ... we keep this as a leeway

        private Dictionary<string, IGrainReminder> allReminders;

        private string filePrefix;

        private Logger logger;
        private string myId; // used to distinguish during debugging between multiple activations of the same grain
        private TimeSpan period;
        private Dictionary<string, long> sequence;

        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            // it can happen that due to failure, when a new activation is created, 
            // it doesn't know which reminders were registered against the grain
            // hence, this activation may receive a reminder that it didn't register itself, but
            // the previous activation (incarnation of the grain) registered... so, play it safe
            if (!sequence.ContainsKey(reminderName))
                sequence.Add(reminderName,
                    0); // we'll get upto date to the latest sequence number while processing this tick

            // calculating tick sequence number

            // we do all arithmetics on DateTime by converting into long because we dont have divide operation on DateTime
            // using dateTime.Ticks is not accurate as between two invocations of ReceiveReminder(), there maybe < period.Ticks
            // if # of ticks between two consecutive ReceiveReminder() is larger than period.Ticks, everything is fine... the problem is when its less
            // thus, we reduce our accuracy by ACCURACY ... here, we are preparing all used variables for the given accuracy
            var now = status.CurrentTickTime.Ticks / ACCURACY; //DateTime.UtcNow.Ticks / ACCURACY;
            var first = status.FirstTickTime.Ticks / ACCURACY;
            var per = status.Period.Ticks / ACCURACY;
            var sequenceNumber = 1 + (now - first) / per;

            // end of calculating tick sequence number

            // do switch-ing here
            if (sequenceNumber < sequence[reminderName])
            {
                logger.Info("ReceiveReminder: {0} Incorrect tick {1} vs. {2} with status {3}.", reminderName,
                    sequence[reminderName], sequenceNumber, status);
                return Task.CompletedTask;
            }

            sequence[reminderName] = sequenceNumber;
            logger.Info("ReceiveReminder: {0} Sequence # {1} with status {2}.", reminderName, sequence[reminderName],
                status);

            var fileName = GetFileName(reminderName);
            var counterValue = sequence[reminderName].ToString(CultureInfo.InvariantCulture);
            File.WriteAllText(fileName, counterValue);

            return Task.CompletedTask;
        }

        public async Task<IGrainReminder> StartReminder(string reminderName, TimeSpan? p = null, bool validate = false)
        {
            var usePeriod = p ?? period;

            //// This is a hack to set the minimum reminder period to 10 sec
            //var ass = Assembly.GetAssembly(typeof(ClientConfiguration));
            //var type = ass.GetType("Orleans.Runtime.Constants");
            //var field = type.GetField("MinReminderPeriod");
            //field.SetValue(null, TimeSpan.FromSeconds(10));

            logger.Info("Starting reminder {0}.", reminderName);
            IGrainReminder r = null;

            if (validate)
                r = await RegisterOrUpdateReminder(reminderName, usePeriod - TimeSpan.FromSeconds(2), usePeriod);
            else
                r = await RegisterOrUpdateReminder(reminderName, usePeriod - TimeSpan.FromSeconds(2), usePeriod);

            allReminders[reminderName] = r;
            sequence[reminderName] = 0;

            var fileName = GetFileName(reminderName);
            File.Delete(fileName); // if successfully started, then remove any old data
            logger.Info("Started reminder {0}", r);
            return r;
        }

        public async Task StopReminder(string reminderName)
        {
            logger.Info("Stopping reminder {0}.", reminderName);
            // we dont reset counter as we want the test methods to be able to read it even after stopping the reminder
            //return UnregisterReminder(allReminders[reminderName]);
            IGrainReminder reminder = null;
            if (allReminders.TryGetValue(reminderName, out reminder))
            {
                await UnregisterReminder(reminder);
            }
            else
            {
                // during failures, there may be reminders registered by an earlier activation that we dont have cached locally
                // therefore, we need to update our local cache 
                await GetMissingReminders();
                if (allReminders.TryGetValue(reminderName, out reminder))
                    await UnregisterReminder(reminder);
                else
                    throw new OrleansException(string.Format(
                        "Could not find reminder {0} in grain {1}", reminderName, IdentityString));
            }
        }


        public async Task StopReminder(IGrainReminder reminder)
        {
            logger.Info("Stopping reminder (using ref) {0}.", reminder);
            // we dont reset counter as we want the test methods to be able to read it even after stopping the reminder
            await UnregisterReminder(reminder);
        }

        public Task<TimeSpan> GetReminderPeriod(string reminderName)
        {
            return Task.FromResult(period);
        }

        public Task<long> GetCounter(string name)
        {
            var fileName = GetFileName(name);
            var data = File.ReadAllText(fileName);
            var counterValue = long.Parse(data);
            return Task.FromResult(counterValue);
        }

        public Task<IGrainReminder> GetReminderObject(string reminderName)
        {
            return GetReminder(reminderName);
        }

        public async Task<List<IGrainReminder>> GetRemindersList()
        {
            return await GetReminders();
        }

        public async Task EraseReminderTable(string connectionString)
        {
            MongoReminderTableRepository repository = new MongoReminderTableRepository(connectionString,
                MongoUrl.Create(connectionString).DatabaseName, null);

            var config = new ClusterConfiguration();
            config.LoadFromFile("ClientConfiguration.xml");

            await repository.RemoveReminderRowsAsync(config.Globals.ServiceId.ToString());

            //Todo: This is the actual method to run, but ReminderTable is an internal class
            //await  ReminderTable.TestOnlyClearTable();
        }

        public override Task OnActivateAsync()
        {
            myId = new Random().Next().ToString(); //this.Data.ActivationId.ToString();// new Random().Next();
            allReminders = new Dictionary<string, IGrainReminder>();
            sequence = new Dictionary<string, long>();
            logger = GetLogger(string.Format("ReminderTestGrain {0}_{1}", RuntimeIdentity, RuntimeIdentity));
            period = GetDefaultPeriod(logger);
            logger.Info("OnActivateAsync.");
            filePrefix = "g" + IdentityString + "_";
            WrongReminderGrain.ExtendTimeOutPeriod();
            return GetMissingReminders();
        }

        public override Task OnDeactivateAsync()
        {
            logger.Info("OnDeactivateAsync");
            return Task.CompletedTask;
        }

        private async Task GetMissingReminders()
        {
            var reminders = await GetReminders();
            logger.Info("Got missing reminders {0}", Utils.EnumerableToString(reminders));
            foreach (var l in reminders)
                if (!allReminders.ContainsKey(l.ReminderName))
                    allReminders.Add(l.ReminderName, l);
        }

        private string GetFileName(string reminderName)
        {
            return string.Format("{0}{1}{2}", AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                filePrefix.Replace("*", "").Replace("/", ""), reminderName);
        }

        public static TimeSpan GetDefaultPeriod(Logger log)
        {
            var period = 10; // Seconds

            var config = new ClusterConfiguration();
            config.LoadFromFile("ClientConfiguration.xml");

            var reminderPeriod = TimeSpan.FromSeconds(period);
            log.Info("Using reminder period of {0} for ReminderServiceType={1} in ReminderTestGrain", reminderPeriod,
                config.Globals.ReminderServiceType);
            return reminderPeriod;
        }
    }

    // NOTE: do not make changes here ... this is a copy of ReminderTestGrain
    // changes to make when copying:
    //      1. rename logger to ReminderCopyGrain
    //      2. filePrefix should start with "gc", instead of "g"
    public class ReminderTestCopyGrain : Grain, IReminderTestCopyGrain, IRemindable
    {
        private static readonly long ACCURACY = 50 * TimeSpan.TicksPerMillisecond
            ; // when we use ticks to compute sequence numbers, we might get wrong results as timeouts don't happen with precision of ticks  ... we keep this as a leeway

        private Dictionary<string, IGrainReminder> allReminders;

        private string filePrefix;

        private Logger logger;
        private long myId; // used to distinguish during debugging between multiple activations of the same grain
        private TimeSpan period;
        private Dictionary<string, long> sequence;

        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            // it can happen that due to failure, when a new activation is created, 
            // it doesn't know which reminders were registered against the grain
            // hence, this activation may receive a reminder that it didn't register itself, but
            // the previous activation (incarnation of the grain) registered... so, play it safe
            if (!sequence.ContainsKey(reminderName))
                sequence.Add(reminderName,
                    0); // we'll get upto date to the latest sequence number while processing this tick

            // calculating tick sequence number

            // we do all arithmetics on DateTime by converting into long because we dont have divide operation on DateTime
            // using dateTime.Ticks is not accurate as between two invocations of ReceiveReminder(), there maybe < period.Ticks
            // if # of ticks between two consecutive ReceiveReminder() is larger than period.Ticks, everything is fine... the problem is when its less
            // thus, we reduce our accuracy by ACCURACY ... here, we are preparing all used variables for the given accuracy
            var now = status.CurrentTickTime.Ticks / ACCURACY; //DateTime.UtcNow.Ticks / ACCURACY;
            var first = status.FirstTickTime.Ticks / ACCURACY;
            var per = status.Period.Ticks / ACCURACY;
            var sequenceNumber = 1 + (now - first) / per;

            // end of calculating tick sequence number

            // do switch-ing here
            if (sequenceNumber < sequence[reminderName])
            {
                logger.Info("{0} Incorrect tick {1} vs. {2} with status {3}.", reminderName, sequence[reminderName],
                    sequenceNumber, status);
                return Task.CompletedTask;
            }

            sequence[reminderName] = sequenceNumber;
            logger.Info("{0} Sequence # {1} with status {2}.", reminderName, sequence[reminderName], status);

            File.WriteAllText(GetFileName(reminderName), sequence[reminderName].ToString());

            return Task.CompletedTask;
        }

        public async Task<IGrainReminder> StartReminder(string reminderName, TimeSpan? p = null, bool validate = false)
        {
            var usePeriod = p ?? period;
            logger.Info("Starting reminder {0} for {1}", reminderName, RuntimeIdentity);
            IGrainReminder r = null;
            if (validate)
                r = await RegisterOrUpdateReminder(reminderName, /*TimeSpan.FromSeconds(3)*/
                    usePeriod - TimeSpan.FromSeconds(2), usePeriod);
            else
                r = await RegisterOrUpdateReminder(reminderName, /*TimeSpan.FromSeconds(3)*/
                    usePeriod - TimeSpan.FromSeconds(2), usePeriod);
            if (allReminders.ContainsKey(reminderName))
            {
                allReminders[reminderName] = r;
                sequence[reminderName] = 0;
            }
            else
            {
                allReminders.Add(reminderName, r);
                sequence.Add(reminderName, 0);
            }

            File.Delete(GetFileName(reminderName)); // if successfully started, then remove any old data
            logger.Info("Started reminder {0}.", r);
            return r;
        }

        public async Task StopReminder(string reminderName)
        {
            logger.Info("Stoping reminder {0}.", reminderName);
            // we dont reset counter as we want the test methods to be able to read it even after stopping the reminder
            //return UnregisterReminder(allReminders[reminderName]);
            IGrainReminder reminder = null;
            if (allReminders.TryGetValue(reminderName, out reminder))
            {
                await UnregisterReminder(reminder);
            }
            else
            {
                // during failures, there may be reminders registered by an earlier activation that we dont have cached locally
                // therefore, we need to update our local cache 
                await GetMissingReminders();
                await UnregisterReminder(allReminders[reminderName]);
            }
        }

        public Task<TimeSpan> GetReminderPeriod(string reminderName)
        {
            return Task.FromResult(period);
        }

        public Task<long> GetCounter(string name)
        {
            return Task.FromResult(long.Parse(File.ReadAllText(GetFileName(name))));
        }

        public override async Task OnActivateAsync()
        {
            myId = new Random().Next();
            allReminders = new Dictionary<string, IGrainReminder>();
            sequence = new Dictionary<string, long>();
            logger = GetLogger(string.Format("ReminderCopyGrain {0}_{1}", myId, myId));
            period = ReminderTestGrain2.GetDefaultPeriod(logger);
            logger.Info("OnActivateAsync.");
            filePrefix = "gc" + IdentityString + "_";

            //*grn / 549BB267 / 4b7d05e0f95eff7cea95b210de1f338903000000549bb267 - 0xC1FA7939
            WrongReminderGrain.ExtendTimeOutPeriod();
            await GetMissingReminders();
        }

        public override Task OnDeactivateAsync()
        {
            logger.Info("OnDeactivateAsync.");
            return Task.CompletedTask;
        }

        private async Task GetMissingReminders()
        {
            var reminders = await GetReminders();
            foreach (var l in reminders)
                if (!allReminders.ContainsKey(l.ReminderName))
                    allReminders.Add(l.ReminderName, l);
        }

        public async Task StopReminder(IGrainReminder reminder)
        {
            logger.Info("Stoping reminder (using ref) {0}.", reminder);
            // we dont reset counter as we want the test methods to be able to read it even after stopping the reminder
            await UnregisterReminder(reminder);
        }

        public async Task<IGrainReminder> GetReminderObject(string reminderName)
        {
            return await GetReminder(reminderName);
        }

        public async Task<List<IGrainReminder>> GetRemindersList()
        {
            return await GetReminders();
        }

        private string GetFileName(string reminderName)
        {
            return string.Format("{0}{1}{2}", AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                filePrefix.Replace("*", "").Replace("/", ""), reminderName);
        }
    }

    public class WrongReminderGrain : Grain, IReminderGrainWrong
    {
        private Logger logger;

        public async Task<bool> StartReminder(string reminderName)
        {
            logger.Info("Starting reminder {0}.", reminderName);
            ExtendTimeOutPeriod();
            var r = await RegisterOrUpdateReminder(reminderName, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));
            logger.Info("Started reminder {0}. It shouldn't have succeeded!", r);
            return true;
        }

        public override Task OnActivateAsync()
        {
            logger = GetLogger(string.Format("WrongReminderGrain_{0}", RuntimeIdentity));
            logger.Info("OnActivateAsync.");
            return Task.CompletedTask;
        }

        public static void ExtendTimeOutPeriod()
        {
            var ass = Assembly.GetAssembly(typeof(ClientConfiguration));
            var type = ass.GetType("Orleans.Runtime.Constants");
            var field = type.GetField("MinReminderPeriod");
            field.SetValue(null, TimeSpan.FromSeconds(10));
            var a = field.GetValue(null);
        }
    }
}

#pragma warning restore 612, 618