// ReSharper disable InheritdocConsiderUsage

namespace Orleans.Providers.MongoDB.Configuration
{
    /// <summary>
    /// Configures MongoDB Reminders Options.
    /// </summary>
    public sealed class MongoDBRemindersOptions : MongoDBOptions
    {
        /// <summary>
        /// Defines the strategy for managing reminders in a MongoDB-based Orleans provider.
        /// </summary>
        /// <remarks>
        /// The value of this property determines the storage and lookup mechanism for reminders.
        /// It can be set to either <see cref="MongoDBReminderStrategy.DefaultStorage"/> to use
        /// a standard storage approach, or <see cref="MongoDBReminderStrategy.HashedLookupStorage"/>
        /// to use a hash-based lookup mechanism for reminders.
        /// Users should select the appropriate strategy based on their application requirements
        /// and performance considerations.
        /// </remarks>
        public MongoDBReminderStrategy Strategy { get; set; } = MongoDBReminderStrategy.DefaultStorage;
        
        public MongoDBRemindersOptions()
        {
        }
    }
}
