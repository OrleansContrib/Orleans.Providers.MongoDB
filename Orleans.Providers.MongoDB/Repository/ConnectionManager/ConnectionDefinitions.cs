using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Repository.ConnectionManager
{
    /// <summary>
    /// The connection definitions.
    /// </summary>
    public class ConnectionDefinitions
    {
        /// <summary>
        /// Gets or sets the database.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        public int Index { get; set; }
    }
}
