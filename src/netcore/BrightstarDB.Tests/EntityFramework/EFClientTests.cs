using System;
using System.Linq;
using System.Text;
using BrightstarDB.Client;
using Xunit;

namespace BrightstarDB.Tests.EntityFramework
{
    [Collection("BrightstarService")]
    public class EFClientTests : IDisposable
    {
        public void Dispose()
        {
            BrightstarService.Shutdown(false);
        }

        [Fact]
        public void TestEmbeddedClientMapToRdf()
        {
            var storeName = "foaf_" + Guid.NewGuid().ToString();
            var embeddedClient =
                BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar;");
            embeddedClient.CreateStore(storeName);

            //add rdf data for a person
            var triples = new StringBuilder();
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://xmlns.com/foaf/0.1/Person> .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/nick> ""Jen"" .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/name> ""Jen Williams"" .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/Organization> ""Networked Planet"" .");

            var job = embeddedClient.ExecuteTransaction(storeName, new UpdateTransactionData{ InsertData = triples.ToString()});
            TestHelper.AssertJobCompletesSuccessfully(embeddedClient, storeName, job);

            //check EF can access all properties
            using (
                var context =
                    new MyEntityContext(string.Format(@"type=embedded;storesDirectory=c:\\brightstar;storeName={0}",
                                                      storeName)))
            {

                Assert.NotNull(context.FoafPersons);
                Assert.Equal(1, context.FoafPersons.Count());
                var person = context.FoafPersons.FirstOrDefault();
                Assert.NotNull(person);

                Assert.NotNull(person.Id);
                Assert.Equal("j.williams", person.Id);
                Assert.NotNull(person.Name);
                Assert.Equal("Jen Williams", person.Name);
                Assert.NotNull(person.Nickname);
                Assert.Equal("Jen", person.Nickname);
                Assert.NotNull(person.Organisation);
                Assert.Equal("Networked Planet", person.Organisation);
            }
        }

        [Fact]
        public void TestMapToRdfDataTypeDate()
        {
            var storeName = "foaf_" + Guid.NewGuid().ToString();
            var embeddedClient =
                BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar;");
            embeddedClient.CreateStore(storeName);

            //add rdf data for a person
            var triples = new StringBuilder();
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://xmlns.com/foaf/0.1/Person> .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://xmlns.com/foaf/0.1/name> ""Jen Williams"" .");
            triples.AppendLine(@"<http://www.networkedplanet.com/people/j.williams> <http://dbpedia.org/ontology/birthDate> ""1921-11-28""^^<http://www.w3.org/2001/XMLSchema#date> .");
            

            var job = embeddedClient.ExecuteTransaction(storeName, new UpdateTransactionData{InsertData = triples.ToString()});
            TestHelper.AssertJobCompletesSuccessfully(embeddedClient, storeName, job);

            //check EF can access all properties
            using (
                var context =
                    new MyEntityContext(string.Format(@"type=embedded;storesDirectory=c:\\brightstar;storeName={0}",
                                                      storeName)))
            {

                Assert.NotNull(context.FoafPersons);
                Assert.Equal(1, context.FoafPersons.Count());
                var person = context.FoafPersons.FirstOrDefault();
                Assert.NotNull(person);

                Assert.NotNull(person.Id);
                Assert.Equal("j.williams", person.Id);
                Assert.NotNull(person.Name);
                Assert.Equal("Jen Williams", person.Name);
                Assert.NotNull(person.BirthDate);
            }
        }

       

    }
}
