using System.IO;
using System.Linq;
using BrightstarDB.Client;
using Xunit;

namespace BrightstarDB.Tests.EntityFramework
{
    
    public class DotNetRdfContextTests
    {
        [Fact]
#if PORTABLE
        [Ignore("DotNetRDF PCL does not support loading files into store configuration")]
#endif
        public void TestInitializeWithStoreConfiguration()
        {
            var configFilePath = Path.Combine(Configuration.DataLocation, "dataObjectStoreConfig.ttl");
            var connectionString = "type=dotNetRdf;configuration=" + configFilePath + ";storeName=http://www.brightstardb.com/tests#people";
            const string baseGraph = "http://example.org/people";

            var context = new MyEntityContext(connectionString, updateGraphUri:baseGraph, datasetGraphUris:new string[]{baseGraph});
            // Can find by property
            var alice = context.FoafPersons.FirstOrDefault(p => p.Name.Equals("Alice"));
            Assert.NotNull(alice);
            // Can find by ID
            alice = context.FoafPersons.FirstOrDefault(p => p.Id.Equals("alice"));
            Assert.NotNull(alice);
            Assert.Equal(alice.Name, "Alice");
            Assert.NotNull(alice.Knows);
        }

        [Fact]
        public void TestInsertIntoDefaultGraph()
        {
            var storeName = "http://www.brightstardb.com/tests#empty";
            var connectionString = MakeStoreConnectionString(storeName);
            var dataObjectContext = BrightstarService.GetDataObjectContext(connectionString);

            string aliceId;
            using (var store = dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(store))
                {
                    var alice = context.FoafPersons.Create();
                    aliceId = alice.Id;
                    context.SaveChanges();
                }
            }
            using (var store = dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(store))
                {
                    var alice = context.FoafPersons.FirstOrDefault(p => p.Id.Equals(aliceId));
                    Assert.NotNull(alice);
                }
            }
        }
    

        private static string MakeStoreConnectionString(string storeName)
        {
            var configFilePath = Path.Combine(Configuration.DataLocation, "dataObjectStoreConfig.ttl");
            return string.Format("type=dotNetRdf;configuration={0};storeName={1}", configFilePath, storeName);
        }
    }
}
