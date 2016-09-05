using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BrightstarDB.Client;
using Xunit;

namespace BrightstarDB.Tests.EntityFramework
{
    [Collection("BrightstarService")]
    public class EntityFrameworkGraphTargetingTests : IDisposable
    {
        private readonly string _storeName = "EntityFrameworkGraphTargetingTests_" + DateTime.Now.Ticks;

        private MyEntityContext NewContext(bool optimisticLocking, string updateGraph = null, IEnumerable<string> datasetGraphs = null, string versioningGraph = null )
        {
            var connectionString =
                String.Format("type=embedded;storesDirectory={0};storeName={1}",
                              Configuration.StoreLocation, _storeName);
            return new MyEntityContext(connectionString, optimisticLocking, updateGraph, datasetGraphs, versioningGraph);
        }

        private IBrightstarService NewRdfClient()
        {
            var connectionString =
                String.Format("type=embedded;storesDirectory={0};storeName={1}",
                              Configuration.StoreLocation, _storeName);
            return BrightstarService.GetClient(connectionString);
        }
        public void Dispose()
        {
            BrightstarService.Shutdown(false);
        }


        [Fact]
        public void TestCreateInNamedGraph()
        {
            const string update = "http://example.org/graphs/update";
            IFoafPerson alice;
            using (var context = NewContext(false, update))
            {
                alice = context.FoafPersons.Create();
                alice.Name = "Alice";
                context.SaveChanges();
            }

            // Triples should be in the update graph
            var client = NewRdfClient();
            var results = client.ExecuteQuery(_storeName,
                                "SELECT ?p ?o ?g FROM NAMED <" + update + "> FROM NAMED <" +
                                Constants.DefaultGraphUri + "> WHERE { GRAPH ?g { <http://www.networkedplanet.com/people/" + alice.Id + "> ?p ?o }}");
            var resultsDoc = XDocument.Load(results);
            Assert.True(resultsDoc.SparqlResultRows().All(r=>r.GetColumnValue("g").ToString().Equals(update)));
        }

        [Fact]
        public void TestAddAndDeletePropertyInSeparateGraph()
        {
            const string inferred = "http://example.org/graphs/inferred";
            IDBPediaPerson woodyAllen;
            using (var context = NewContext(false))
            {
                woodyAllen = context.DBPediaPersons.Create();
                woodyAllen.GivenName = "Woody";
                woodyAllen.Surname = "Allen";
                context.SaveChanges();
            }

            using (var context = NewContext(false, inferred))
            {
                var woodyAllen2 = context.DBPediaPersons.FirstOrDefault(p => p.Id.Equals(woodyAllen.Id));
                Assert.NotNull(woodyAllen2);
                Assert.Equal("Woody", woodyAllen2.GivenName);
                Assert.Equal("Allen", woodyAllen2.Surname);
                Assert.Null(woodyAllen2.Name);
                woodyAllen2.Name = woodyAllen2.GivenName + " " + woodyAllen2.Surname;
                context.SaveChanges();

                // Name triple should be in the inferred graph
                var client = NewRdfClient();
                var results = client.ExecuteQuery(_storeName,
                                                  "SELECT ?p ?o ?g FROM NAMED <" + inferred + ">" +
                                                  " WHERE { GRAPH ?g { <http://dbpedia.org/resource/" + woodyAllen2.Id +
                                                  "> ?p ?o }}");
                var resultsDoc = XDocument.Load(results);
                var rows = resultsDoc.SparqlResultRows().ToList();
                Assert.Equal(1, rows.Count);
                Assert.Equal(inferred, rows[0].GetColumnValue("g").ToString());
                Assert.Equal("http://xmlns.com/foaf/0.1/name", rows[0].GetColumnValue("p").ToString());
                Assert.Equal("Woody Allen", rows[0].GetColumnValue("o").ToString());

                // Remove property should delete from the graph where the property is stored
                woodyAllen2.Name = null;
                woodyAllen2.Surname = null;
                context.SaveChanges();

                // Inferred graph should now be empy
                results = client.ExecuteQuery(_storeName,
                                              "SELECT ?p ?o ?g FROM NAMED <" + inferred + ">" +
                                              " WHERE { GRAPH ?g { <http://dbpedia.org/resource/" + woodyAllen2.Id +
                                              "> ?p ?o }}");
                resultsDoc = XDocument.Load(results);
                rows = resultsDoc.SparqlResultRows().ToList();
                Assert.Equal(0, rows.Count);
            }
            using (var context = NewContext(false))
            {
                var woodyAllen4 = context.DBPediaPersons.FirstOrDefault(p => p.Id.Equals(woodyAllen.Id));
                Assert.NotNull(woodyAllen4);
                Assert.Equal("Woody", woodyAllen4.GivenName);
                Assert.Null(woodyAllen4.Surname);
                Assert.Null(woodyAllen4.Name);
            }
        }
    }
}
