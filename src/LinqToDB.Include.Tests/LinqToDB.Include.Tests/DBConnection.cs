using LinqToDB.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace App.Domain.Repository.Mapping
{

    public class ConnectionStringSettings : IConnectionStringSettings
    {
        public string ConnectionString { get; set; }
        public string Name { get; set; }
        public string ProviderName { get; set; }
        public bool IsGlobal => false;
    }

    public class DBConnection : ILinqToDBSettings
    {
        public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

        public string DefaultConfiguration => "SQLite";
        public string DefaultDataProvider => "SqlServer";
        
        public DBConnection()
        {
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json");

            var config = builder.Build();
            
            ConnectionStrings = config.GetSection("Connections").Get<ConnectionStringSettings[]>();
        }

        public IEnumerable<IConnectionStringSettings> ConnectionStrings { get; }        
    }
}
