using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.EntityFramework;
using BrightstarDB.EntityFramework.Query;
using Xunit;
using System.ComponentModel;
using System.Reflection;
using BrightstarDB.Query;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Algebra;
#if !PORTABLE
using System.ComponentModel.DataAnnotations;
#endif

namespace BrightstarDB.Tests.EntityFramework
{
    [Collection("BrightstarService")]
    public class SimpleContextTests : IDisposable
    {
        private readonly IDataObjectContext _dataObjectContext;
        public SimpleContextTests()
        {
            var connectionString = new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation);
            _dataObjectContext = new EmbeddedDataObjectContext(connectionString);
        }

        public void Dispose()
        {
            BrightstarService.Shutdown(false);
        }

        [Fact]
        public void TestCreateAndRetrieve()
        {
            string storeName = Guid.NewGuid().ToString();
            string personId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.Create();
                    Assert.NotNull(person);
                    context.SaveChanges();
                    Assert.NotNull(person.Id);
                    personId = person.Id;
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                    Assert.NotNull(person);
                }
            }
        }

        [Fact]
        public void TestShortcutCreate()
        {
            string storeName = "TestShortcutCreate" + DateTime.Now.Ticks;
            string aliceId;
            using (var context = CreateEntityContext(storeName))
            {
                var sales = new Department(context) {Name = "Sales", DeptId = 1};
                var bob = new Person(context) {Name = "Bob"};
                var alice = new Person(context) {Name = "Alice", Age = 54, Department = sales, Friends = new Collection<IPerson>{bob}};
                context.SaveChanges();
                aliceId = alice.Id;
            }

            using (var context = CreateEntityContext(storeName))
            {
                var alice = context.Persons.FirstOrDefault(x => x.Id == aliceId);
                Assert.NotNull(alice);
                Assert.Equal("Alice", alice.Name);
                Assert.Equal(54, alice.Age);
                Assert.NotNull(alice.Department);
                Assert.Equal("Sales", alice.Department.Name);
                Assert.Equal(1, alice.Friends.Count);
                Assert.Equal("Bob", alice.Friends.First().Name);
            }
        }

        [Fact]
        public void TestContextAddMethod()
        {
            var storeName = "TestContextAddMethod_" + DateTime.UtcNow.Ticks;
            string aliceId;
            string gingerId;
            using (var context = CreateEntityContext(storeName))
            {
                var alice = new Person {Name = "Alice", Age = 25};
                var ginger = new Animal{Name="Ginger", Owner = alice};
                context.Add(alice);
                context.Add(ginger);
                context.SaveChanges();
                aliceId = alice.Id;
                gingerId = ginger.Id;
            }
            Assert.NotNull(aliceId);
            using (var context = CreateEntityContext(storeName))
            {
                var alice = context.Persons.FirstOrDefault(x => x.Id == aliceId);
                Assert.NotNull(alice);
                Assert.Equal(25, alice.Age);
                Assert.NotNull(alice.Pet);
                Assert.Equal(gingerId, alice.Pet.Id);
                Assert.Equal("Ginger", alice.Pet.Name);
            }
        }

        [Fact]
        public void TestContextAddRangeMethod()
        {
            var storeName = "TestContextAddRangeMethod_" + DateTime.UtcNow.Ticks;
            string aliceId, gingerId;
            using (var context = CreateEntityContext(storeName))
            {
                var alice = new Person { Name = "Alice", Age = 25 };
                var ginger = new Animal { Name = "Ginger" };
                context.AddRange(new object[] {alice, ginger});
                context.SaveChanges();
                aliceId = alice.Id;
                gingerId = ginger.Id;
            }
            Assert.NotNull(aliceId);
            Assert.NotNull(gingerId);
            using (var context = CreateEntityContext(storeName))
            {
                var alice = context.Persons.FirstOrDefault(x => x.Id == aliceId);
                Assert.NotNull(alice);
                Assert.Equal(25, alice.Age);
                var ginger = context.Animals.FirstOrDefault(x => x.Id == gingerId);
                Assert.NotNull(ginger);
                Assert.Equal("Ginger", ginger.Name);
            }
        }

        [Fact]
        public void TestContextAddOrUpdateRangeMethod()
        {
            var storeName = "TestContextAddOrUpdateRangeMethod_" + DateTime.UtcNow.Ticks;
            string aliceId, gingerId;
            using (var context = CreateEntityContext(storeName))
            {
                var alice = new Person { Name = "Alice", Age = 25 };
                var ginger = new Animal { Name = "Ginger" };
                context.AddRange(new object[] { alice, ginger });
                context.SaveChanges();
                aliceId = alice.Id;
                gingerId = ginger.Id;
            }
            Assert.NotNull(aliceId);
            Assert.NotNull(gingerId);
            using (var context = CreateEntityContext(storeName))
            {
                var alice= new Person{Id=aliceId, Name="Updated Alice", Age=26};
                var ginger = new Animal{Id=gingerId, Name = "Updated Ginger"};
                context.AddOrUpdateRange(new object[] {alice, ginger});
                context.SaveChanges();
            }
            using (var context = CreateEntityContext(storeName))
            {
                var alice = context.Persons.FirstOrDefault(x => x.Id == aliceId);
                Assert.NotNull(alice);
                Assert.Equal(26, alice.Age);
                var ginger = context.Animals.FirstOrDefault(x => x.Id == gingerId);
                Assert.NotNull(ginger);
                Assert.Equal("Updated Ginger", ginger.Name);
            }
        }


        [Fact]
        public void TestAddOrUpdateWithGeneratedId()
        {
            var storeName = "TestAddOrUpdateWithGeneratedId" + DateTime.UtcNow.Ticks;
            var alice = new Person{Name="Alice", Age=25};
            using (var context = CreateEntityContext(storeName))
            {
                context.Persons.AddOrUpdate(alice);
                context.SaveChanges();
            }
            Assert.NotNull(alice.Id);
            var updateAlice = new Person {Id = alice.Id, Name = "UpdatedAlice", Age = 26};
            using (var context = CreateEntityContext(storeName))
            {
                context.Persons.AddOrUpdate(updateAlice);
                context.SaveChanges();
            }
            Assert.Equal(alice.Id, updateAlice.Id);
            using (var context = CreateEntityContext(storeName))
            {
                var people = context.Persons.ToList();
                Assert.Equal(1, people.Count);
                var p = people[0];
                Assert.Equal("UpdatedAlice", p.Name);
                Assert.Equal(26, p.Age);
            }
        }

        [Fact]
        public void TestAddOrUpdateWithSimpleKey()
        {
            var storeName = "TestAddOrUpdateWithSimpleKey" + DateTime.UtcNow.Ticks;
            var alice = new StringKeyEntity { Name = "alice", Description = "Alice Entity"};
            using (var context = CreateEntityContext(storeName))
            {
                context.StringKeyEntities.AddOrUpdate(alice);
                context.SaveChanges();
            }
            Assert.NotNull(alice.Id);
            Assert.Equal("alice", alice.Id);
            var updateAlice = new StringKeyEntity{ Id = "alice", Description= "UpdatedAlice Entity"};
            using (var context = CreateEntityContext(storeName))
            {
                context.StringKeyEntities.AddOrUpdate(updateAlice);
                context.SaveChanges();
            }
            Assert.Equal("alice", updateAlice.Id);
            using (var context = CreateEntityContext(storeName))
            {
                var people = context.StringKeyEntities.ToList();
                Assert.Equal(1, people.Count);
                var p = people[0];
                Assert.Equal("alice", p.Name);
                Assert.Equal("UpdatedAlice Entity", p.Description);
            }
        }

        [Fact]
        public void TestAddOrUpdateWithCompositeKey()
        {
            var storeName = "TestAddOrUpdateWithCompositeKey" + DateTime.UtcNow.Ticks;
            var foo1 = new CompositeKeyEntity { First = "foo", Second = 1, Description="This is a test" };
            using (var context = CreateEntityContext(storeName))
            {
                context.CompositeKeyEntities.AddOrUpdate(foo1);
                context.SaveChanges();
            }
            Assert.NotNull(foo1.Id);
            Assert.Equal("foo.1", foo1.Id);
            var updatedFoo1 = new CompositeKeyEntity{ First = "foo", Second = 1, Description = "This is an updated test" };
            using (var context = CreateEntityContext(storeName))
            {
                context.CompositeKeyEntities.AddOrUpdate(updatedFoo1);
                context.SaveChanges();
            }
            Assert.Equal("foo.1", updatedFoo1.Id);
            using (var context = CreateEntityContext(storeName))
            {
                var people = context.CompositeKeyEntities.ToList();
                Assert.Equal(1, people.Count);
                var p = people[0];
                Assert.Equal("foo", p.First);
                Assert.Equal(1, p.Second);
                Assert.Equal("This is an updated test", p.Description);
            }
        }

        [Fact]
        public void TestCustomTriplesQuery()
        {
            var storeName = Guid.NewGuid().ToString();
            var people = new Person[10];
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var person = new Person { Age = 40 - i, Name = "Person #" + i };
                        context.Persons.Add(person);
                        people[i] = person;
                    }
                    context.SaveChanges();
                }
            }


            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var query = @"
select  ?s ?p ?o
where {
   ?s ?p ?o.
        ?s a <http://www.example.org/schema/Person>
}
";
                    IList<Person> results;
                    results = context.ExecuteQuery<Person>(query).ToList();
                    Assert.Equal(10, results.Count);

                    foreach (Person person in results)
                    {
                        Assert.NotEqual(0, person.Age);
                    }
                }
            }
        }

        [Fact]
        public void TestCustomTriplesQueryWithOrderedSubjects()
        {
            string storeName = Guid.NewGuid().ToString();
            var people = new Person[10];
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var person = new Person { Age = 40 - i, Name = "Person #" + i };
                        context.Persons.Add(person);
                        people[i] = person;
                    }
                    context.SaveChanges();
                }
            }


            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var query = @"
select  ?s ?p ?o
where {
   ?s ?p ?o.
        ?s a <http://www.example.org/schema/Person>
}
order by ?s
";
                    IList<Person> results;
                    results = context.ExecuteQuery<Person>(new SparqlQueryContext(query){ExpectTriplesWithOrderedSubjects = true}).ToList();
                    Assert.Equal(10, results.Count);

                    foreach (Person person in results)
                    {
                        Assert.NotEqual(0, person.Age);
                    }
                }
            }
        }


        [Fact]
        public void TestCustomTriplesQueryWithMultipleResultTypes()
        {
            string storeName = Guid.NewGuid().ToString();
            var people = new Person[10];
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var person = new Person { Age = 40 - i, Name = "Person #" + i };
                        context.Persons.Add(person);
                        people[i] = person;

                        if (i >= 5)
                        {
                            var session = new Session { Speaker = "Person #" + i };
                            context.Sessions.Add(session);
                        }
                    }
                    context.SaveChanges();
                }
            }


            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var query = @"
select  ?s ?p ?o
where {
   ?s ?p ?o.
   {?s a <http://www.example.org/schema/Person>}
    union
   {?s a <http://www.example.org/schema/Session>}

}

";
                    var results = context.ExecuteQueryToResultTypes(query);
                    var persons = results[typeof(Person)].Cast<Person>().ToList();
                    Assert.Equal(10, persons.Count);

                    var sessions = results[typeof(Session)].Cast<Session>().ToList();
                    Assert.Equal(5, sessions.Count);

                }
            }
        }

        [Fact]
        public void TestGetSetOfEntitiesById()
        {
            var storeName = "TestGetSetOfEntitiesById_" + DateTime.Now.Ticks;
            using (var context = CreateEntityContext(storeName))
            {
                context.Persons.Add(new Person{Id="alice", Name = "Alice"});
                context.Persons.Add(new Person { Id = "bob", Name = "Bob" });
                context.Persons.Add(new Person { Id = "carol", Name = "Carol" });
                context.SaveChanges();
            }

            using (var context = CreateEntityContext(storeName))
            {
                var results =
                    context.Persons.Where(x => new string[] {"alice", "bob", "carol", "david"}.Contains(x.Id)).ToList();
                Assert.Equal(3, results.Count);
                Assert.True(results.Any(x=>x.Id.Equals("alice") && x.Name.Equals("Alice")));
                Assert.True(results.Any(x => x.Id.Equals("bob") && x.Name.Equals("Bob")));
                Assert.True(results.Any(x => x.Id.Equals("carol") && x.Name.Equals("Carol")));
            }
        }

        [Fact]
        public void TestSetAndGetSimpleProperty()
        {
            string storeName = Guid.NewGuid().ToString();
            string personId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.Create();
                    Assert.NotNull(person);
                    person.Name = "Kal";
                    context.SaveChanges();
                    personId = person.Id;
                }
            }

            // Test that the property is still there when we retrieve the object again
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                    Assert.NotNull(person);
                    Assert.NotNull(person.Name);
                    Assert.Equal("Kal", person.Name);
                }
            }

            // Test we can also use the simple property in a LINQ query
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.Name == "Kal");
                    Assert.NotNull(person);
                    Assert.Equal(personId, person.Id);

                    // Test we can use ToList()
                    var people = context.Persons.Where(p => p.Name == "Kal").ToList();
                    Assert.NotNull(people);
                    Assert.Equal(1, people.Count);
                    Assert.Equal(personId, people[0].Id);
                }
            }
        }

        [Fact]
        public void TestOrderingOfResults()
        {
            string storeName = Guid.NewGuid().ToString();
            var peterDob = DateTime.Now.AddYears(-35);
            var anneDob = DateTime.Now.AddYears(-28);
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var joDob = DateTime.Now.AddYears(-34);
                    var mirandaDob = DateTime.Now.AddYears(-32);

                    var jo = context.Persons.Create();
                    Assert.NotNull(jo);
                    jo.Name = "Jo";
                    jo.DateOfBirth = joDob;
                    jo.Age = 34;
                    var peter = context.Persons.Create();
                    Assert.NotNull(peter);
                    peter.Name = "Peter";
                    peter.DateOfBirth = peterDob;
                    peter.Age = 35;
                    var miranda = context.Persons.Create();
                    Assert.NotNull(miranda);
                    miranda.Name = "Miranda";
                    miranda.DateOfBirth = mirandaDob;
                    miranda.Age = 32;
                    var anne = context.Persons.Create();
                    Assert.NotNull(anne);
                    anne.Name = "Anne";
                    anne.DateOfBirth = anneDob;
                    anne.Age = 28;

                    context.SaveChanges();
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var people = context.Persons.ToList();
                    Assert.Equal(4, people.Count);

                    var orderedByName = context.Persons.OrderBy(p => p.Name).ToList();
                    var orderedByAge = context.Persons.OrderBy(p => p.Age).ToList();
                    var orderedByDob = context.Persons.OrderBy(p => p.DateOfBirth).ToList();

                    Assert.Equal("Anne", orderedByName[0].Name);
                    Assert.Equal("Peter", orderedByName[3].Name);
                    Assert.Equal(28, orderedByAge[0].Age);
                    Assert.Equal(35, orderedByAge[3].Age);
                    Assert.Equal(peterDob, orderedByDob[0].DateOfBirth);
                    Assert.Equal(anneDob, orderedByDob[3].DateOfBirth);
                }
            }

        }

        [Fact]
        public void TestGetAndSetDateTimeProperty()
        {
            string storeName = Guid.NewGuid().ToString();
            string personId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var person = context.Persons.Create();
                Assert.NotNull(person);
                person.Name = "Kal";
                person.DateOfBirth = new DateTime(1970, 12, 12);
                context.SaveChanges();
                personId = person.Id;
            }

            // Test that the property is still there when we retrieve the object again
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                    Assert.NotNull(person);
                    Assert.NotNull(person.Name);
                    Assert.Equal("Kal", person.Name);
                    Assert.True(person.DateOfBirth.HasValue);
                    Assert.Equal(1970, person.DateOfBirth.Value.Year);
                    Assert.Equal(12, person.DateOfBirth.Value.Month);
                    Assert.Equal(12, person.DateOfBirth.Value.Day);
                }
            }

            // Test we can also use the simple property in a LINQ query
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.DateOfBirth == new DateTime(1970, 12, 12));
                    Assert.NotNull(person);
                    Assert.Equal(personId, person.Id);

                    // Test we can set a nullable property back to null
                    person.DateOfBirth = null;
                    context.SaveChanges();
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.Name.Equals("Kal"));
                    Assert.NotNull(person);
                    Assert.Null(person.DateOfBirth);
                }
            }
        }

        [Fact]
        public void TestLoopThroughEntities()
        {
            string storeName = Guid.NewGuid().ToString();
            string homerId, bartId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var bart = context.Persons.Create();
                    bart.Name = "Bart Simpson";
                    var homer = context.Persons.Create();
                    homer.Name = "Homer Simpson";
                    bart.Father = homer;

                    var marge = context.Persons.Create();
                    marge.Name = "Marge Simpson";
                    bart.Mother = marge;

                    context.SaveChanges();
                    homerId = homer.Id;
                    bartId = bart.Id;
                }
            }

            // Query with results converted to a list
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var homersKids = context.Persons.Where(p => p.Father.Id == homerId).ToList();
                    Assert.Equal(1, homersKids.Count);
                    Assert.Equal(bartId, homersKids.First().Id);
                }
            }
        }

        [Fact]
        public void TestSetAndGetSingleRelatedObject()
        {
            string storeName = Guid.NewGuid().ToString();
            string bartId, homerId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var bart = context.Persons.Create();
                bart.Name = "Bart Simpson";
                var homer = context.Persons.Create();
                homer.Name = "Homer Simpson";
                bart.Father = homer;
                context.SaveChanges();
                homerId = homer.Id;
                bartId = bart.Id;
            }

            // Test that the property is still there when we retrieve the object again
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var bart = context.Persons.FirstOrDefault(p => p.Id == bartId);
                    Assert.NotNull(bart);
                    var bartFather = bart.Father;
                    Assert.NotNull(bartFather);
                    Assert.Equal(homerId, bartFather.Id);
                }
            }
            // See if we can use the property in a query
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var homersKids = context.Persons.Where(p => p.Father.Id == homerId).ToList();
                    Assert.Equal(1, homersKids.Count);
                    Assert.Equal(bartId, homersKids.First().Id);
                }
            }
        }

        [Fact]
        public void TestPopulateEntityCollectionWithExistingEntities()
        {
            string storeName = Guid.NewGuid().ToString();
            string aliceId, bobId, carolId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.Create();
                alice.Name = "Alice";
                var bob = context.Persons.Create();
                bob.Name = "Bob";
                var carol = context.Persons.Create();
                carol.Name = "Carol";
                alice.Friends.Add(bob);
                alice.Friends.Add(carol);
                context.SaveChanges();
                aliceId = alice.Id;
                bobId = bob.Id;
                carolId = carol.Id;
            }

            // See if we can access the collection on a loaded object
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var alice = context.Persons.FirstOrDefault(p => p.Id == aliceId);
                    Assert.NotNull(alice);
                    var friends = alice.Friends as IEntityCollection<IPerson>;
                    Assert.NotNull(friends);
                    Assert.False(friends.IsLoaded);
                    friends.Load();
                    Assert.True(friends.IsLoaded);
                    Assert.Equal(2, alice.Friends.Count);
                    Assert.True(alice.Friends.Any(p => p.Id.Equals(bobId)));
                    Assert.True(alice.Friends.Any(p => p.Id.Equals(carolId)));
                }
            }
        }

        [Fact]
        public void TestSetEntityCollection()
        {
            string storeName = Guid.NewGuid().ToString();
            string aliceId, bobId, carolId, daveId, edwinaId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.Create();
                var bob = context.Persons.Create();
                var carol = context.Persons.Create();
                alice.Friends = new List<IPerson> {bob, carol};
                context.SaveChanges();

                aliceId = alice.Id;
                bobId = bob.Id;
                carolId = carol.Id;
            }

            // See if we can access the collection on a loaded object
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var alice = context.Persons.FirstOrDefault(p => p.Id == aliceId);
                    Assert.NotNull(alice);
                    var friends = alice.Friends as IEntityCollection<IPerson>;
                    Assert.NotNull(friends);
                    Assert.False(friends.IsLoaded);
                    friends.Load();
                    Assert.True(friends.IsLoaded);
                    Assert.Equal(2, alice.Friends.Count);
                    Assert.True(alice.Friends.Any(p => p.Id.Equals(bobId)));
                    Assert.True(alice.Friends.Any(p => p.Id.Equals(carolId)));
                    var dave = context.Persons.Create();
                    var edwina = context.Persons.Create();
                    alice.Friends = new List<IPerson> {dave, edwina};
                    context.SaveChanges();
                    daveId = dave.Id;
                    edwinaId = edwina.Id;
                }
            }

            // See if we can access the collection on a loaded object
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var alice = context.Persons.FirstOrDefault(p => p.Id == aliceId);
                    Assert.NotNull(alice);
                    var friends = alice.Friends as IEntityCollection<IPerson>;
                    Assert.NotNull(friends);
                    Assert.False(friends.IsLoaded);
                    friends.Load();
                    Assert.True(friends.IsLoaded);
                    Assert.Equal(2, alice.Friends.Count);
                    Assert.True(alice.Friends.Any(p => p.Id.Equals(daveId)));
                    Assert.True(alice.Friends.Any(p => p.Id.Equals(edwinaId)));
                }
            }
        }

        
        [Fact]
        public void TestSetRelatedEntitiesToNullThrowsArgumentNullException()
        {
            var storeName = "TestSetRelatedEntitiesToNullThrowsArgumentNullException_" + DateTime.Now.Ticks;
            string aliceId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.Create();
                var bob = context.Persons.Create();
                var carol = context.Persons.Create();
                alice.Friends = new List<IPerson> { bob, carol };
                context.SaveChanges();
                aliceId = alice.Id;
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId));
                Assert.NotNull(alice);
                Assert.Equal(2, alice.Friends.Count);
                Assert.Throws<ArgumentNullException>(() => alice.Friends = null);
            }
        }

        [Fact]
        public void TestClearRelatedObjects()
        {
            var storeName = "TestClearRelatedObjects_" + DateTime.Now.Ticks;
            string aliceId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.Create();
                var bob = context.Persons.Create();
                var carol = context.Persons.Create();
                alice.Friends = new List<IPerson> { bob, carol };
                context.SaveChanges();
                aliceId = alice.Id;
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId));
                Assert.NotNull(alice);
                Assert.Equal(2, alice.Friends.Count);
                alice.Friends.Clear();
                Assert.Equal(0, alice.Friends.Count);
                context.SaveChanges();
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId));
                Assert.NotNull(alice);
                Assert.Equal(0, alice.Friends.Count);
            }
        }

        [Fact]
        public void TestOneToOneInverse()
        {
            string storeName = Guid.NewGuid().ToString();
            string aliceId, bobId, carolId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var alice = context.Persons.Create();
                    alice.Name = "Alice";
                    var bob = context.Animals.Create();
                    alice.Pet = bob;
                    context.SaveChanges();
                    aliceId = alice.Id;
                    bobId = bob.Id;
                }
            }

            // See if we can access the inverse property
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var bob = context.Animals.FirstOrDefault(a => a.Id.Equals(bobId));
                    Assert.NotNull(bob);
                    Assert.NotNull(bob.Owner);
                    Assert.Equal(aliceId, bob.Owner.Id);

                    // see if we can access an item by querying against its inverse property
                    bob = context.Animals.FirstOrDefault(a => a.Owner.Id.Equals(aliceId));
                    Assert.NotNull(bob);

                    // check that alice.Pet refers to the same object as bob
                    bob.Name = "Bob";
                    Assert.NotNull(bob.Name);
                    Assert.Equal("Bob", bob.Name);
                    var alice = context.Persons.FirstOrDefault(a => a.Id.Equals(aliceId));
                    Assert.NotNull(alice);
                    var alicePet = alice.Pet;
                    Assert.NotNull(alicePet);
                    Assert.Equal(bob, alicePet);
                    Assert.Equal("Bob", alicePet.Name);

                    // Transfer object by changing the forward property
                    var carol = context.Persons.Create();
                    carol.Name = "Carol";
                    carol.Pet = bob;
                    carolId = carol.Id;
                    Assert.Equal(carol, bob.Owner);
                    Assert.Null(alice.Pet);
                    context.SaveChanges();
                }
            }

            // Check that changes to forward properties get persisted
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var bob = context.Animals.FirstOrDefault(a => a.Owner.Id.Equals(carolId));
                    Assert.NotNull(bob);
                    Assert.Equal("Bob", bob.Name);
                    var alice = context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId));
                    Assert.NotNull(alice);
                    Assert.Null(alice.Pet);
                    var carol = context.Persons.FirstOrDefault(p => p.Id.Equals(carolId));
                    Assert.NotNull(carol);
                    Assert.NotNull(carol.Pet);
                    Assert.Equal(bob, carol.Pet);

                    // Transfer object by changing inverse property
                    bob.Owner = alice;
                    Assert.NotNull(alice.Pet);
                    Assert.Null(carol.Pet);
                    context.SaveChanges();
                }
            }

            // Check that changes to inverse properties get persisted
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var bob = context.Animals.FirstOrDefault(a => a.Id.Equals(bobId));
                    Assert.NotNull(bob);
                    Assert.Equal("Bob", bob.Name);
                    Assert.Equal(aliceId, bob.Owner.Id);
                    var alice = context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId));
                    Assert.NotNull(alice);
                    Assert.NotNull(alice.Pet);
                    Assert.Equal(bob, alice.Pet);
                    var carol = context.Persons.FirstOrDefault(p => p.Id.Equals(carolId));
                    Assert.NotNull(carol);
                    Assert.Null(carol.Pet);
                }
            }
        }

        [Fact]
        public void TestManyToManyInverse()
        {
            string storeName = Guid.NewGuid().ToString();
            string aliceId, bobId, jsId, cssId, jqueryId, rdfId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.Create();
                alice.Name = "Alice";
                var bob = context.Persons.Create();
                bob.Name = "Bob";
                var js = context.Skills.Create();
                js.Name = "Javascript";
                var css = context.Skills.Create();
                css.Name = "CSS";
                var jquery = context.Skills.Create();
                jquery.Name = "JQuery";
                var rdf = context.Skills.Create();
                rdf.Name = "RDF";

                alice.Skills.Add(js);
                alice.Skills.Add(css);
                bob.Skills.Add(js);
                bob.Skills.Add(jquery);
                context.SaveChanges();

                aliceId = alice.Id;
                bobId = bob.Id;
                jsId = js.Id;
                cssId = css.Id;
                jqueryId = jquery.Id;
                rdfId = rdf.Id;
            }

            // See if we can access the inverse properties correctly
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var js = context.Skills.FirstOrDefault(x => x.Id.Equals(jsId));
                    Assert.NotNull(js);
                    Assert.Equal(2, js.SkilledPeople.Count);
                    Assert.True(js.SkilledPeople.Any(x => x.Id.Equals(aliceId)));
                    Assert.True(js.SkilledPeople.Any(x => x.Id.Equals(bobId)));
                    var css = context.Skills.FirstOrDefault(x => x.Id.Equals(cssId));
                    Assert.NotNull(css);
                    Assert.Equal(1, css.SkilledPeople.Count);
                    Assert.True(css.SkilledPeople.Any(x => x.Id.Equals(aliceId)));
                    var jquery = context.Skills.FirstOrDefault(x => x.Id.Equals(jqueryId));
                    Assert.NotNull(jquery);
                    Assert.Equal(1, jquery.SkilledPeople.Count);
                    Assert.True(jquery.SkilledPeople.Any(x => x.Id.Equals(bobId)));
                    var rdf = context.Skills.FirstOrDefault(x => x.Id.Equals(rdfId));
                    Assert.NotNull(rdf);
                    Assert.Equal(0, rdf.SkilledPeople.Count);

                    //  Test adding to an inverse property with some existing values and an inverse property with no existing values
                    var bob = context.Persons.FirstOrDefault(x => x.Id.Equals(bobId));
                    Assert.NotNull(bob);
                    var alice = context.Persons.FirstOrDefault(x => x.Id.Equals(aliceId));
                    Assert.NotNull(alice);
                    css.SkilledPeople.Add(bob);
                    Assert.Equal(2, css.SkilledPeople.Count);
                    Assert.True(css.SkilledPeople.Any(x => x.Id.Equals(bobId)));
                    Assert.Equal(3, bob.Skills.Count);
                    Assert.True(bob.Skills.Any(x => x.Id.Equals(cssId)));
                    rdf.SkilledPeople.Add(alice);
                    Assert.Equal(1, rdf.SkilledPeople.Count);
                    Assert.True(rdf.SkilledPeople.Any(x => x.Id.Equals(aliceId)));
                    Assert.Equal(3, alice.Skills.Count);
                    Assert.True(alice.Skills.Any(x => x.Id.Equals(rdfId)));
                }
            }
        }

        [Fact]
        public void TestManyToOneInverse()
        {
            string storeName = Guid.NewGuid().ToString();
            string rootId, skillAId, skillBId, childSkillId, childSkill2Id;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {

                    var root = context.Skills.Create();
                    root.Name = "Root";
                    var skillA = context.Skills.Create();
                    skillA.Parent = root;
                    skillA.Name = "Skill A";
                    var skillB = context.Skills.Create();
                    skillB.Parent = root;
                    skillB.Name = "Skill B";

                    Assert.NotNull(root.Children);
                    Assert.Equal(2, root.Children.Count);
                    Assert.True(root.Children.Any(x => x.Id.Equals(skillA.Id)));
                    Assert.True(root.Children.Any(x => x.Id.Equals(skillB.Id)));
                    context.SaveChanges();

                    rootId = root.Id;
                    skillAId = skillA.Id;
                    skillBId = skillB.Id;
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var root = context.Skills.FirstOrDefault(x => x.Id.Equals(rootId));
                    Assert.NotNull(root);
                    var childSkill = context.Skills.Create();
                    childSkill.Name = "Child Skill";
                    childSkill.Parent = root;
                    Assert.Equal(3, root.Children.Count);
                    Assert.True(root.Children.Any(x => x.Id.Equals(childSkill.Id)));
                    Assert.True(root.Children.Any(x => x.Id.Equals(skillAId)));
                    Assert.True(root.Children.Any(x => x.Id.Equals(skillBId)));
                    context.SaveChanges();
                    childSkillId = childSkill.Id;
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var root = context.Skills.FirstOrDefault(x => x.Id.Equals(rootId));
                    Assert.NotNull(root);
                    Assert.Equal(3, root.Children.Count);
                    Assert.True(root.Children.Any(x => x.Id.Equals(childSkillId)));
                    Assert.True(root.Children.Any(x => x.Id.Equals(skillAId)));
                    Assert.True(root.Children.Any(x => x.Id.Equals(skillBId)));
                    var childSkill2 = context.Skills.Create();
                    childSkill2.Name = "Child Skill 2";
                    childSkill2.Parent = root;
                    Assert.Equal(4, root.Children.Count);
                    Assert.True(root.Children.Any(x => x.Id.Equals(childSkill2.Id)));
                    context.SaveChanges();
                    childSkill2Id = childSkill2.Id;
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var root = context.Skills.FirstOrDefault(x => x.Id.Equals(rootId));
                    var skillA = context.Skills.FirstOrDefault(x => x.Id.Equals(skillAId));
                    var childSkill2 = context.Skills.FirstOrDefault(x => x.Id.Equals(childSkill2Id));
                    Assert.NotNull(root);
                    Assert.NotNull(skillA);
                    Assert.NotNull(childSkill2);
                    Assert.Equal(4, root.Children.Count);
                    Assert.True(root.Children.Any(x => x.Id.Equals(childSkillId)));
                    Assert.True(root.Children.Any(x => x.Id.Equals(skillAId)));
                    Assert.True(root.Children.Any(x => x.Id.Equals(skillBId)));
                    Assert.True(root.Children.Any(x => x.Id.Equals(childSkill2Id)));
                    // Move a skill to a new parent
                    childSkill2.Parent = skillA;
                    Assert.Equal(3, root.Children.Count);
                    Assert.False(root.Children.Any(x => x.Id.Equals(childSkill2Id)));
                    Assert.Equal(1, skillA.Children.Count);
                    Assert.True(skillA.Children.Any(x => x.Id.Equals(childSkill2Id)));
                    context.SaveChanges();
                }
            }

            // Check the move has persisted
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var root = context.Skills.FirstOrDefault(x => x.Id.Equals(rootId));
                    var skillA = context.Skills.FirstOrDefault(x => x.Id.Equals(skillAId));
                    var childSkill2 = context.Skills.FirstOrDefault(x => x.Id.Equals(childSkill2Id));
                    Assert.NotNull(root);
                    Assert.NotNull(skillA);
                    Assert.NotNull(childSkill2);
                    Assert.Equal(3, root.Children.Count);
                    Assert.False(root.Children.Any(x => x.Id.Equals(childSkill2Id)));
                    Assert.Equal(1, skillA.Children.Count);
                    Assert.True(skillA.Children.Any(x => x.Id.Equals(childSkill2Id)));
                    Assert.Equal(skillA.Id, childSkill2.Parent.Id);
                    Assert.Equal(root.Id, childSkill2.Parent.Parent.Id);
                }
            }
        }
       
        /// <summary>
        /// Tests for a property that in its forward direction is a one-to-many property (i.e. a collection)
        /// and in its inverse is a many-to-one property (i.e. a single-valued property).
        /// </summary>
        [Fact]
        public void TestOneToManyInverse()
        {
            string storeName = "SimpleContextTests.TestOneToManyInverse_" + DateTime.Now.Ticks;
            var connectionString = "type=embedded;storesDirectory=c:\\brightstar;storeName=" + storeName;
            string market1Id, market2Id, companyAId, companyBId, companyCId, companyDId;
            using (var context = new MyEntityContext(connectionString))
            {

                var market1 = context.Markets.Create();
                var market2 = context.Markets.Create();
                market1.Name = "Market1";
                market2.Name = "Market2";
                var companyA = context.Companies.Create();
                var companyB = context.Companies.Create();
                var companyC = context.Companies.Create();
                companyA.Name = "CompanyA";
                companyB.Name = "CompanyB";
                companyC.Name = "CompanyC";

                market1Id = market1.Id;
                market2Id = market2.Id;
                companyAId = companyA.Id;
                companyBId = companyB.Id;
                companyCId = companyC.Id;

                market1.ListedCompanies.Add(companyA);
                market2.ListedCompanies.Add(companyB);

                Assert.Equal(market1, companyA.ListedOn);
                Assert.Equal(market2, companyB.ListedOn);
                Assert.Null(companyC.ListedOn);
                context.SaveChanges();
            }
            using (var context = new MyEntityContext(connectionString))
            {
                var market1 = context.Markets.FirstOrDefault(x => x.Id.Equals(market1Id));
                var market2 = context.Markets.FirstOrDefault(x => x.Id.Equals(market2Id));
                var companyA = context.Companies.FirstOrDefault(x => x.Id.Equals(companyAId));
                var companyB = context.Companies.FirstOrDefault(x => x.Id.Equals(companyBId));
                var companyC = context.Companies.FirstOrDefault(x => x.Id.Equals(companyCId));
                Assert.NotNull(market1);
                Assert.NotNull(market2);
                Assert.NotNull(companyA);
                Assert.NotNull(companyB);
                Assert.NotNull(companyC);
                Assert.Equal(market1, companyA.ListedOn);
                Assert.Equal(market2, companyB.ListedOn);
                Assert.Null(companyC.ListedOn);

                // Add item to collection
                market1.ListedCompanies.Add(companyC);

                Assert.Equal(market1, companyA.ListedOn);
                Assert.Equal(market1, companyC.ListedOn);
                Assert.Equal(2, market1.ListedCompanies.Count);
                Assert.True(market1.ListedCompanies.Any(x => x.Id.Equals(companyA.Id)));
                Assert.True(market1.ListedCompanies.Any(x => x.Id.Equals(companyC.Id)));
                context.SaveChanges();
            }

            using (var context = new MyEntityContext(connectionString))
            {
                var market1 = context.Markets.FirstOrDefault(x => x.Id.Equals(market1Id));
                var market2 = context.Markets.FirstOrDefault(x => x.Id.Equals(market2Id));
                var companyA = context.Companies.FirstOrDefault(x => x.Id.Equals(companyAId));
                var companyB = context.Companies.FirstOrDefault(x => x.Id.Equals(companyBId));
                var companyC = context.Companies.FirstOrDefault(x => x.Id.Equals(companyCId));
                Assert.NotNull(market1);
                Assert.NotNull(market2);
                Assert.NotNull(companyA);
                Assert.NotNull(companyB);
                Assert.NotNull(companyC);

                Assert.Equal(market1, companyA.ListedOn);
                Assert.Equal(market1, companyC.ListedOn);
                Assert.Equal(2, market1.ListedCompanies.Count);
                Assert.True(market1.ListedCompanies.Any(x => x.Id.Equals(companyA.Id)));
                Assert.True(market1.ListedCompanies.Any(x => x.Id.Equals(companyC.Id)));

                // Set the single-valued inverse property
                var companyD = context.Companies.Create();
                companyD.Name = "CompanyD";
                companyD.ListedOn = market2;
                companyDId = companyD.Id;

                Assert.Equal(market2, companyB.ListedOn);
                Assert.Equal(market2, companyD.ListedOn);
                Assert.Equal(2, market2.ListedCompanies.Count);
                Assert.True(market2.ListedCompanies.Any(x => x.Id.Equals(companyB.Id)));
                Assert.True(market2.ListedCompanies.Any(x => x.Id.Equals(companyD.Id)));
                context.SaveChanges();
            }
            using (var context = new MyEntityContext(connectionString))
            {
                var market2 = context.Markets.FirstOrDefault(x => x.Id.Equals(market2Id));
                var companyB = context.Companies.FirstOrDefault(x => x.Id.Equals(companyBId));
                var companyD = context.Companies.FirstOrDefault(x => x.Id.Equals(companyDId));
                Assert.NotNull(market2);
                Assert.NotNull(companyB);
                Assert.NotNull(companyD);
                Assert.Equal(market2, companyB.ListedOn);
                Assert.Equal(market2, companyD.ListedOn);
                Assert.Equal(2, market2.ListedCompanies.Count);
                Assert.True(market2.ListedCompanies.Any(x => x.Id.Equals(companyB.Id)));
                Assert.True(market2.ListedCompanies.Any(x => x.Id.Equals(companyD.Id)));
            }
        }


        [Fact]
        public void TestSetContextAndIdentityProperties()
        {
            string storeName = Guid.NewGuid().ToString();
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = new Person
                        {
                            Context = context,
                            Id = "http://example.org/people/123",
                            Name = "Kal",
                            DateOfBirth = new DateTime(1970, 12, 12)
                        };

                    context.SaveChanges();
                }
            }
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var found =
                        context.Persons.FirstOrDefault(p => p.Id.Equals("http://example.org/people/123"));
                    Assert.NotNull(found);
                }
            }
        }

        [Fact]
        public void TestSetPropertiesThenAttach()
        {
            string storeName = Guid.NewGuid().ToString();
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    // ReSharper disable UseObjectOrCollectionInitializer
                    // Purposefully setting properties and then attaching Context property
                    var person = new Person
                        {
                            Name = "Kal",
                            DateOfBirth = new DateTime(1970, 12, 12),
                            Friends = new List<IPerson>
                                {
                                    new Person {Name = "Gra", Id = "http://example.org/people/1234"},
                                    new Person {Name = "Stu", Id = "http://example.org/people/456"}
                                }
                        };
                    person.Id = "http://example.org/people/123";
                    person.Context = context;
                    // ReSharper restore UseObjectOrCollectionInitializer
                    context.SaveChanges();
                }
            }
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var found =
                        context.Persons.FirstOrDefault(p => p.Id.Equals("http://example.org/people/123"));
                    Assert.NotNull(found);
                    Assert.Equal("Kal", found.Name);
                    Assert.Equal(new DateTime(1970, 12, 12), found.DateOfBirth);

                    found = context.Persons.FirstOrDefault(p => p.Id.Equals("http://example.org/people/1234"));
                    Assert.NotNull(found);
                    Assert.Equal("Gra", found.Name);

                    found = context.Persons.FirstOrDefault(p => p.Id.Equals("http://example.org/people/456"));
                    Assert.NotNull(found);
                    Assert.Equal("Stu", found.Name);
                }
            }
        }

        [Fact]
        public void TestBaseResourceAddress()
        {
            string storeName = Guid.NewGuid().ToString();
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var skill = new Skill {Id = "foo", Name = "Foo"};
                    context.Skills.Add(skill);
                    var otherSkill = new Skill {Id = "bar", Name = "Bar", Context = context};
                    var yetAnotherSkill = new Skill {Name = "Bletch"};
                    context.Skills.Add(yetAnotherSkill);
                    context.SaveChanges();

                    var found = context.Skills.FirstOrDefault(s => s.Id.Equals("foo"));
                    Assert.NotNull(found);
                    Assert.Equal("foo", found.Id);
                    Assert.Equal("Foo", found.Name);

                    found = context.Skills.FirstOrDefault(s => s.Id.Equals("bar"));
                    Assert.NotNull(found);
                    Assert.Equal("bar", found.Id);
                    Assert.Equal("Bar", found.Name);

                    found = context.Skills.FirstOrDefault(s => s.Name.Equals("Bletch"));
                    Assert.NotNull(found);
                    Guid foundId;
                    Assert.True(Guid.TryParse(found.Id, out foundId));
                }
            }
        }

        //[Ignore]
        [Fact]
        public void TestAddGeneratesIdentity()
        {
            string storeName = Guid.NewGuid().ToString();
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var skill = new Skill {Name = "Bar"};
                    var person = new Person {Name = "Kal"};
                    //var person2 = new Person2 {Name = "Jen"};
                    var company = new Company {Name = "NetworkedPlanet"};
                    context.Persons.Add(person);
                    //context.Person2s.Add(person2);
                    context.Skills.Add(skill);
                    context.Companies.Add(company);
                    context.SaveChanges();
                }
            }
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var foundPerson = context.Persons.FirstOrDefault(p => p.Name.Equals("Kal"));
                    //var foundPerson2 = context.Person2s.Where(p => p.Name.Equals("Jen")).FirstOrDefault();
                    var foundSkill = context.Skills.FirstOrDefault(s => s.Name.Equals("Bar"));
                    var foundCompany = context.Companies.FirstOrDefault(s => s.Name.Equals("NetworkedPlanet"));
                    Assert.NotNull(foundPerson);
                    //Assert.NotNull(foundPerson2);
                    Assert.NotNull(foundSkill);
                    Assert.NotNull(foundCompany);

                    // Generated Ids should be GUIDs
                    Guid g;
                    Assert.True(Guid.TryParse(foundPerson.Id, out g));
                    Assert.True(Guid.TryParse(foundCompany.Id, out g));
                    Assert.True(Guid.TryParse(foundSkill.Id, out g));
                }
            }
        }

        [Fact]
        public void TestIdentifierPrefix()
        {
            var dataStoreName = "TestIdentifierPrefix_" + DateTime.Now.Ticks;
            using (var dataStore = _dataObjectContext.CreateStore(dataStoreName))
            {
                using (var context = new MyEntityContext(dataStore))
                {
                    var fido = context.Animals.Create();
                    fido.Name = "Fido";
                    var foafPerson = context.FoafPersons.Create();
                    foafPerson.Name = "Bob";
                    var skill = context.Skills.Create();
                    skill.Name = "Testing";
                    var company = context.Companies.Create();
                    company.Name = "BrightstarDB";
                    context.SaveChanges();
                }
                var fidoDo = dataStore.BindDataObjectsWithSparql(
                    "SELECT ?f WHERE { ?f a <http://www.example.org/schema/Animal> }").FirstOrDefault();
                Assert.NotNull(fidoDo);
                Assert.True(fidoDo.Identity.StartsWith("http://brightstardb.com/instances/Animals/"));
            }
        }

        [Fact]
        public void TestEmptyStringIdentifierPrefix()
        {
            var dataStoreName = "TestEmptyStringIdentifierPrefix_" + DateTime.UtcNow.Ticks;
            using (var dataStore = _dataObjectContext.CreateStore(dataStoreName))
            {
                using (var context = new MyEntityContext(dataStore))
                {
                    var fido = new UriEntity {Id = "http://brightstardb.com/instances/Animals/fido", Label = "Fido"};
                    context.UriEntities.Add(fido);
                    var bob = new UriEntity(context) { Id = "http://example.org/people/bob", Label = "Bob" };
                    context.SaveChanges();
                }
                var fidoDo =
                    dataStore.BindDataObjectsWithSparql(
                        "SELECT ?f WHERE { ?f <http://www.w3.org/2000/01/rdf-schema#label> \"Fido\"^^<http://www.w3.org/2001/XMLSchema#string> }").FirstOrDefault();
                Assert.NotNull(fidoDo);
                Assert.Equal(fidoDo.Identity, "http://brightstardb.com/instances/Animals/fido");
                using (var context = new MyEntityContext(dataStore))
                {
                    var fido =
                        context.UriEntities.FirstOrDefault(
                            x => x.Id.Equals("http://brightstardb.com/instances/Animals/fido"));
                    Assert.NotNull(fido);
                    Assert.Equal("Fido", fido.Label);

                    var bob = context.UriEntities.FirstOrDefault(x => x.Label.Equals("Bob"));
                    Assert.NotNull(bob);
                    Assert.Equal("http://example.org/people/bob", bob.Id);
                }
            }
        }

        [Fact]
        public void TestCreateMethodGeneratesValidUriForEmptyIdentifierPrefix()
        {
            var dataStoreName = "TestEmptyStringIdentifierPrefix_" + DateTime.UtcNow.Ticks;
            using (var dataStore = _dataObjectContext.CreateStore(dataStoreName))
            {
                using (var context = new MyEntityContext(dataStore))
                {
                    var test = context.UriEntities.Create();
                    test.Label = "Test";
                    context.SaveChanges();
                }
                var testDo =
                    dataStore.BindDataObjectsWithSparql(
                        "SELECT ?f WHERE { ?f <http://www.w3.org/2000/01/rdf-schema#label> \"Test\"^^<http://www.w3.org/2001/XMLSchema#string> }")
                        .FirstOrDefault();
                Assert.NotNull(testDo);
                Assert.StartsWith(Constants.GeneratedUriPrefix, testDo.Identity);
            }
        }

        [Fact]
        public void TestSkipAndTake()
        {
            string storeName = Guid.NewGuid().ToString();
            var people = new Person[10];
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var person = new Person {Age = 40 - i, Name = "Person #" + i};
                        context.Persons.Add(person);
                        people[i] = person;
                    }
                    context.SaveChanges();
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {

                    // Take, skip and skip and take with no other query expression
                    var top3 = context.Persons.Take(3).ToList();
                    Assert.Equal(3, top3.Count);
                    foreach (var p in top3)
                    {
                        Assert.True(people.Any(x => p.Id.Equals(x.Id)));
                    }
                    var after3 = context.Persons.Skip(3).ToList();
                    Assert.Equal(7, after3.Count);
                    var nextPage = context.Persons.Skip(3).Take(3).ToList();
                    Assert.Equal(3, nextPage.Count);

                    // Combined with a sort expression
                    var top3ByAge = context.Persons.OrderByDescending(p => p.Age).Take(3).ToList();
                    Assert.Equal(3, top3ByAge.Count);
                    foreach (var p in top3ByAge) Assert.True(p.Age >= 38);

                    var allButThreeOldest = context.Persons.OrderByDescending(p => p.Age).Skip(3).ToList();
                    Assert.Equal(7, allButThreeOldest.Count);
                    foreach (var p in allButThreeOldest) Assert.False(p.Age >= 38);

                    var nextThreeOldest = context.Persons.OrderByDescending(p => p.Age).Skip(3).Take(3).ToList();
                    Assert.Equal(3, nextThreeOldest.Count);
                    foreach (var p in nextThreeOldest) Assert.True(p.Age < 38 && p.Age > 34);
                }
            }
        }

        [Fact]
        public void TestConnectionString()
        {
            var storeName = Guid.NewGuid().ToString();
            BrightstarService.GetClient("type=embedded;storesdirectory=c:\\brightstar").CreateStore(storeName);
            string personId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var person = context.Persons.Create();
                Assert.NotNull(person);
                context.SaveChanges();
                Assert.NotNull(person.Id);
                personId = person.Id;
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                Assert.NotNull(person);
            }
        }

        [Fact]
        public void TestConnectionStringCreatesStore()
        {
            var storeName = Guid.NewGuid().ToString();
            string personId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var person = context.Persons.Create();
                Assert.NotNull(person);
                context.SaveChanges();
                Assert.NotNull(person.Id);
                personId = person.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                Assert.NotNull(person);
            }
        }


        [Fact]
        public void TestMultipleConnections()
        {
            var storeName = Guid.NewGuid().ToString();
            string personId;
            var client = BrightstarService.GetClient("type=embedded;storesdirectory=c:\\brightstar");
            client.CreateStore(storeName);

            // TODO: Reinstate this when GetStoreData is added back to the service interface
            //client.GetStoreData(storeName).Close();
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var person = context.Persons.Create();
                Assert.NotNull(person);
                context.SaveChanges();
                Assert.NotNull(person.Id);
                personId = person.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                Assert.NotNull(person);
            }
        }

        [Fact]
        public void TestSetTwoInverse()
        {
            var storeName = "TestSetTwoInverse_" + DateTime.Now.Ticks;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var market = context.Markets.Create();
                var company1 = context.Companies.Create();
                var company2 = context.Companies.Create();
                company1.ListedOn = market;
                company2.ListedOn = market;
                context.SaveChanges();

                market = context.Markets.FirstOrDefault();
                Assert.NotNull(market);
                Assert.NotNull(market.ListedCompanies);
                Assert.Equal(2, market.ListedCompanies.Count);
                var company3 = context.Companies.Create();
                market.ListedCompanies.Add(company3);
                context.SaveChanges();
            }
        }

        [Fact]
        public void TestSetCollectionWithManyToOneInverse()
        {
            var storeName = "TestSetCollectionWithManyToOneInverse_" + DateTime.Now.Ticks;
            string marketId;
            using (var context = CreateEntityContext(storeName))
            {
                var market = new Market
                {
                    ListedCompanies = new[]
                    {
                        new Company {Name = "CompanyA"},
                        new Company {Name = "CompanyB"},
                        new Company {Name = "CompanyC"},
                    }
                };
                context.Markets.Add(market);
                context.SaveChanges();
                marketId = market.Id;
            }

            using (var context = CreateEntityContext(storeName))
            {
                var market = context.Markets.FirstOrDefault(x => x.Id.Equals(marketId));
                Assert.NotNull(market);
                Assert.Equal(3, market.ListedCompanies.Count);
                Assert.True(market.ListedCompanies.Any(x => x.Name.Equals("CompanyA")));
                Assert.True(market.ListedCompanies.Any(x => x.Name.Equals("CompanyB")));
                Assert.True(market.ListedCompanies.Any(x => x.Name.Equals("CompanyC")));
            }
        }

        [Fact]
        public void TestQueryOnPrefixedIdentifier()
        {
            var storeName = Guid.NewGuid().ToString();
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var skill = new Skill {Name = "Fencing", Id = "fencing"};
                context.Skills.Add(skill);
                context.SaveChanges();

                var skillId = skill.Id;
                Assert.NotNull(skillId);
                Assert.Equal("fencing", skill.Id);

                var foundSkill = context.Skills.FirstOrDefault(s => s.Id.Equals(skillId));
                Assert.NotNull(foundSkill);
                Assert.Equal("Fencing", foundSkill.Name);
            }
        }

        [Fact]
        public void TestGreaterThanLessThan()
        {
            var storeName = Guid.NewGuid().ToString();
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var apple = context.Companies.Create();
                apple.Name = "Apple";
                apple.CurrentMarketCap = 1.0;
                apple.HeadCount = 150000;

                var ibm = context.Companies.Create();
                ibm.Name = "IBM";
                ibm.CurrentMarketCap = 2.0;
                ibm.HeadCount = 200000;

                var np = context.Companies.Create();
                np.Name = "NetworkedPlanet";
                np.CurrentMarketCap = 3.0;
                np.HeadCount = 4;

                context.SaveChanges();

                var smallCompanies = context.Companies.Where(x => x.HeadCount < 10).ToList();
                Assert.Equal(1, smallCompanies.Count);
                Assert.Equal(np.Id, smallCompanies[0].Id);

                var bigCompanies = context.Companies.Where(x => x.HeadCount > 1000).ToList();
                Assert.Equal(2, bigCompanies.Count);
                Assert.True(bigCompanies.Any(x => x.Id.Equals(apple.Id)));
                Assert.True(bigCompanies.Any(x => x.Id.Equals(ibm.Id)));
            }
        }

        [Fact]
        public void TestSetAndGetLiteralsCollection()
        {
            var storeName = Guid.NewGuid().ToString();
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var agent7 = context.FoafAgents.Create();
                agent7.MboxSums.Add("mboxsum1");
                agent7.MboxSums.Add("mboxsum2");
                context.SaveChanges();
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var agent7 = context.FoafAgents.FirstOrDefault();
                Assert.NotNull(agent7);
                Assert.Equal(2, agent7.MboxSums.Count);
                Assert.True(agent7.MboxSums.Any(x => x.Equals("mboxsum1")));
                Assert.True(agent7.MboxSums.Any(x => x.Equals("mboxsum2")));
                agent7.MboxSums = new List<string> {"replacement1", "replacement2", "replacement3"};
                context.SaveChanges();
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var agent7 = context.FoafAgents.FirstOrDefault();
                Assert.NotNull(agent7);
                Assert.Equal(3, agent7.MboxSums.Count);
                Assert.True(agent7.MboxSums.Any(x => x.Equals("replacement1")));
                Assert.True(agent7.MboxSums.Any(x => x.Equals("replacement2")));
                Assert.True(agent7.MboxSums.Any(x => x.Equals("replacement3")));

                var found = context.FoafAgents.Where(x => x.MboxSums.Contains("replacement2"));
                Assert.NotNull(found);

                agent7.MboxSums.Clear();
                context.SaveChanges();
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var agent7 = context.FoafAgents.FirstOrDefault();
                Assert.NotNull(agent7);
                Assert.Equal(0, agent7.MboxSums.Count);
            }
        }

        [Fact]
        public void TestSetByteArray()
        {
            var storeName = "SetByteArray_" + Guid.NewGuid();
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var testEntity = context.TestEntities.Create();
                testEntity.SomeByteArray = new byte[] {0, 1, 2, 3, 4};
                context.SaveChanges();
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var e = context.TestEntities.FirstOrDefault();
                Assert.NotNull(e);
                Assert.NotNull(e.SomeByteArray);
                Assert.Equal(5, e.SomeByteArray.Count());
                for (byte i = 0; i < 5; i++)
                {
                    Assert.Equal(i, e.SomeByteArray[i]);
                }
            }
        }

        [Fact]
        public void TestSetGuid()
        {
            var storeName = "SetGuid_" + DateTime.Now.Ticks;
            var testGuid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
            using (var doStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(doStore))
                {
                    var testEntity = context.TestEntities.Create();
                    testEntity.SomeGuid = testGuid;
                    context.SaveChanges();
                }
            }
            using (var doStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(doStore))
                {
                    var testEntity = context.TestEntities.FirstOrDefault();
                    Assert.NotNull(testEntity);
                    var testEntityId = testEntity.Id;
                    Assert.Equal(testGuid, testEntity.SomeGuid);

                    // Verify we can use a Guid value in a search
                    testEntity = context.TestEntities.FirstOrDefault(e => e.SomeGuid.Equals(testGuid));
                    Assert.NotNull(testEntity);
                    Assert.Equal(testEntityId, testEntity.Id);
                    Assert.Null(context.TestEntities.FirstOrDefault(e=>e.SomeGuid.Equals(Guid.Empty)));
                }
            }
        }

        [Fact]
        public void TestGuidAndNullableGuidDefaults()
        {
            var storeName = "TestGuidAndNullableGuidDefaults_" + DateTime.Now.Ticks;
            string testEntityId;
            using (var doStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(doStore))
                {
                    var testEntity = context.TestEntities.Create();
                    testEntityId = testEntity.Id;
                    context.SaveChanges();
                }
            }
            using (var doStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(doStore))
                {
                    var testEntity = context.TestEntities.FirstOrDefault(x => x.Id.Equals(testEntityId));
                    Assert.NotNull(testEntity);
                    Assert.NotNull(testEntity.SomeGuid);
                    Assert.Equal(Guid.Empty, testEntity.SomeGuid);
                    Assert.False(testEntity.SomeNullableGuid.HasValue);
                }
            }
        }

        [Fact]
        public void TestSetEnumeration()
        {
            var storeName = "SetEnumeration_" + DateTime.Now.Ticks;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var testEntity = context.TestEntities.Create();
                testEntity.SomeEnumeration = TestEnumeration.Third;
                context.SaveChanges();
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var e = context.TestEntities.FirstOrDefault();
                Assert.NotNull(e);
                Assert.Equal(TestEnumeration.Third, e.SomeEnumeration);
            }
        }

        [Fact]
        public void TestQueryOnEnumeration()
        {
            var storeName = "QueryEnumeration_" + DateTime.Now.Ticks;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var entity1 = context.TestEntities.Create();
                var entity2 = context.TestEntities.Create();
                var entity3 = context.TestEntities.Create();
                entity1.SomeString = "Entity1";
                entity1.SomeEnumeration = TestEnumeration.First;
                entity2.SomeString = "Entity2";
                entity2.SomeEnumeration = TestEnumeration.Second;
                entity3.SomeString = "Entity3";
                entity3.SomeEnumeration = TestEnumeration.Second;
                context.SaveChanges();

                Assert.Equal(1,
                                context.TestEntities.Count(e => e.SomeEnumeration == TestEnumeration.First));
                Assert.Equal(2,
                                context.TestEntities.Count(e => e.SomeEnumeration == TestEnumeration.Second));
                Assert.Equal(0,
                                context.TestEntities.Count(e => e.SomeEnumeration == TestEnumeration.Third));
            }
        }

        [Fact]
        public void TestOptimisticLocking()
        {
            var storeName = "TestOptimisticLocking_" + DateTime.Now.Ticks;
            string personId;
            using (
                var context = new MyEntityContext(
                    "type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName, true))
            {
                var person = context.Persons.Create();
                context.SaveChanges();
                personId = person.Id;
            }

            using (
                var context1 = new MyEntityContext(
                    "type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName, true))
            {
                var person1 = context1.Persons.FirstOrDefault(p => p.Id == personId);
                Assert.NotNull(person1);

                using (
                    var context2 = new MyEntityContext(
                        "type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName, true))
                {
                    var person2 = context2.Persons.FirstOrDefault(p => p.Id == personId);
                    Assert.NotNull(person2);

                    Assert.NotSame(person2, person1);

                    person1.Name = "bob";
                    person2.Name = "berby";

                    context1.SaveChanges();
                    Assert.Throws<TransactionPreconditionsFailedException>(() => context2.SaveChanges());
                }
            }
        }

        [Fact]
        public void TestDeleteEntity()
        {
            var storeName = Guid.NewGuid().ToString();
            string jenId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                // create person
                var p1 = context.Persons.Create();
                p1.Name = "jen";
                context.SaveChanges();

                // retrieve object
                jenId = p1.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var jen = context.Persons.FirstOrDefault(p => p.Id == jenId);

                context.DeleteObject(jen);
                context.SaveChanges();

                jen = context.Persons.FirstOrDefault(p => p.Id == jenId);
                Assert.Null(jen);
            }
        }

        [Fact]
        public void TestDeleteEntityInSameContext()
        {
            var storeName = "DeleteEntityInSameContext_" + DateTime.Now.Ticks;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var alice = context.Persons.Create();
                alice.Name = "Alice";
                context.SaveChanges();

                string aliceId = alice.Id;

                // Delete object
                context.DeleteObject(alice);
                context.SaveChanges();

                // Object should no longer be discoverable
                Assert.Null(context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId)));
            }
        }

        [Fact]
        public void TestDeletionOfEntities()
        {
            var storeName = Guid.NewGuid().ToString();
            string jenId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var p1 = context.Persons.Create();
                p1.Name = "jen";

                var skillIds = new List<string>();
                for (var i = 0; i < 5; i++)
                {
                    var skill = context.Skills.Create();
                    skill.Name = "Skill " + i;
                    if (i < 3)
                    {
                        p1.Skills.Add(skill);
                    }
                    skillIds.Add(skill.Id);
                }
                context.SaveChanges();
                jenId = p1.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var jen = context.Persons.FirstOrDefault(p => p.Id.Equals(jenId));

                Assert.NotNull(jen);
                Assert.Equal("jen", jen.Name);
                Assert.Equal(3, jen.Skills.Count);

                var allSkills = context.Skills;
                Assert.Equal(5, allSkills.Count());
                foreach (var s in allSkills)
                {
                    context.DeleteObject(s);
                }
                context.SaveChanges();
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var allSkills = context.Skills;
                Assert.Equal(0, allSkills.Count());

                var jen = context.Persons.FirstOrDefault(p => p.Id.Equals(jenId));

                Assert.NotNull(jen);
                Assert.Equal("jen", jen.Name);
                Assert.Equal(0, jen.Skills.Count);

                context.DeleteObject(jen);
                context.SaveChanges();

                jen = context.Persons.FirstOrDefault(p => p.Id.Equals(jenId));
                Assert.Null(jen);
            }
        }

#if !PORTABLE
        [Fact]
        public void TestGeneratedPropertyAttributes()
        {
            var foafPerson = typeof(FoafPerson);
            var nameProperty = foafPerson.GetProperty("Name");
            Assert.NotNull(nameProperty);
            var requiredAttribute = nameProperty.GetCustomAttributes<RequiredAttribute>().FirstOrDefault();
            Assert.NotNull(requiredAttribute);
            var customValidation = nameProperty.GetCustomAttributes<CustomValidationAttribute>(false).FirstOrDefault();
            Assert.NotNull(customValidation);
            Assert.Equal(typeof(MyCustomValidator), customValidation.ValidatorType);
            Assert.Equal("ValidateName", customValidation.Method);
            Assert.Equal("Custom error message", customValidation.ErrorMessage);

            var nickNameProperty = foafPerson.GetProperty("Nickname");
            Assert.NotNull(nickNameProperty);
            var displayName = nickNameProperty.GetCustomAttributes<DisplayNameAttribute>(false).FirstOrDefault();
            Assert.NotNull(displayName);
            Assert.Equal("Also Known As", displayName.DisplayName);

            var dobProperty = foafPerson.GetProperty("BirthDate");
            Assert.NotNull(dobProperty);
            var datatype = dobProperty.GetCustomAttributes<DataTypeAttribute>().FirstOrDefault();
            Assert.NotNull(datatype);
            Assert.Equal(DataType.Date, datatype.DataType);
        }
#endif

        [Fact]
        public void TestGeneratedClassAttributes()
        {
            var foafPerson = typeof(FoafPerson);
            var displayAttributes = foafPerson.GetTypeInfo().GetCustomAttributes<DisplayNameAttribute>(false).ToList();
            Assert.Equal(1, displayAttributes.Count);
            Assert.Equal("Person", displayAttributes[0].DisplayName);
        }

        [Fact]
        public void TestSingleUriProperty()
        {
            var storeName = "TestSingleUriProperty_" + DateTime.Now.Ticks;
            string personId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var person = context.FoafPersons.Create();
                person.Name = "Kal Ahmed";
                person.Homepage = new Uri("http://www.techquila.com/");
                context.SaveChanges();
                personId = person.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var retrieved = context.FoafPersons.FirstOrDefault(p => p.Id.Equals(personId));
                Assert.NotNull(retrieved);
                Assert.Equal("Kal Ahmed", retrieved.Name);
                Assert.Equal(new Uri("http://www.techquila.com/"), retrieved.Homepage);
            }
        }

        [Fact]
        public void TestUriCollectionProperty()
        {
            var storeName = "TestUriCollectionProperty_" + DateTime.Now.Ticks;
            string personId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var person = context.Persons.Create();
                person.Name = "Kal Ahmed";
                person.Websites.Add(new Uri("http://www.techquila.com/"));
                person.Websites.Add(new Uri("http://brightstardb.com/"));
                person.Websites.Add(new Uri("http://www.networkedplanet.com/"));
                context.SaveChanges();

                personId = person.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var retrieved = context.Persons.FirstOrDefault(p => p.Id.Equals(personId));
                Assert.NotNull(retrieved);
                Assert.Equal("Kal Ahmed", retrieved.Name);
                Assert.Equal(3, retrieved.Websites.Count);
                Assert.True(retrieved.Websites.Any(w => w.Equals(new Uri("http://www.techquila.com/"))));
                retrieved.Websites.Remove(new Uri("http://www.techquila.com/"));
                context.SaveChanges();
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var retrieved = context.Persons.FirstOrDefault(p => p.Id.Equals(personId));
                Assert.NotNull(retrieved);
                Assert.Equal("Kal Ahmed", retrieved.Name);
                Assert.Equal(2, retrieved.Websites.Count);
                Assert.False(retrieved.Websites.Any(w => w.Equals(new Uri("http://www.techquila.com/"))));
                Assert.True(retrieved.Websites.Contains(new Uri("http://brightstardb.com/")));
            }
        }

        [Fact]
        public void TestCollectionUpdatedByInverseProperty()
        {
            var storeName = "TestCollectionUpdatedByInverseProperty_" + DateTime.Now.Ticks;
            using (var context = CreateEntityContext(storeName))
            {
                var dept = new Department {Name = "Research"};
                context.Departments.Add(dept);
                
                // Attach before property is set
                var alice = new Person {Name = "Alice"};
                context.Persons.Add(alice);
                alice.Department = dept;
                Assert.Equal(1, dept.Persons.Count);
                
                // Attach after property set
                var bob = new Person {Name = "Bob", Department = dept};
                context.Persons.Add(bob);
                Assert.Equal(2, dept.Persons.Count);

                // Attach after property set by explicit call
                var charlie = new Person { Name = "Charlie"};
                charlie.Department = dept;
                context.Persons.Add(charlie);
                Assert.Equal(3, dept.Persons.Count);

                // Not attached before checking inverse property
                var dave = new Person { Name = "Dave", Department = dept };
                Assert.Equal(3, dept.Persons.Count);
                context.Persons.Add(dave);
                Assert.Equal(4, dept.Persons.Count);
                
                context.SaveChanges();

                Assert.Equal(4, dept.Persons.Count);
                context.DeleteObject(bob);
                Assert.Equal(3, dept.Persons.Count);
                context.SaveChanges();

                Assert.Equal(3, dept.Persons.Count);
            }
        }

        [Fact]
        public void TestEntitySetsHelper()
        {
            var storeName = "TestEntitySetsHelper" + DateTime.Now.Ticks;
            string pid;
            using (var context = CreateEntityContext(storeName))
            {
                var p = context.Persons.Create();
                var b = context.BaseEntities.Create();
                var d = context.DerivedEntities.Create();
                context.SaveChanges();

                // Test that we can use the returned entity set for query
                var personSet = context.EntitySet<IPerson>();
                Assert.NotNull(personSet);
                Assert.NotNull(personSet.FirstOrDefault(x=>x.Id.Equals(p.Id)));
                Assert.Null(personSet.FirstOrDefault(x=>x.Id.Equals(b.Id)));
                Assert.Null(personSet.FirstOrDefault(x => x.Id.Equals(d.Id)));

                var baseEntitySet = context.EntitySet<IBaseEntity>();
                Assert.NotNull(baseEntitySet);
                Assert.Null(baseEntitySet.FirstOrDefault(x => x.Id.Equals(p.Id)));
                Assert.NotNull(baseEntitySet.FirstOrDefault(x => x.Id.Equals(b.Id)));
                Assert.NotNull(baseEntitySet.FirstOrDefault(x => x.Id.Equals(d.Id)));

                var derivedEntitySet = context.EntitySet<IDerivedEntity>();
                Assert.NotNull(derivedEntitySet);
                Assert.Null(derivedEntitySet.FirstOrDefault(x => x.Id.Equals(p.Id)));
                Assert.Null(derivedEntitySet.FirstOrDefault(x => x.Id.Equals(b.Id)));
                Assert.NotNull(derivedEntitySet.FirstOrDefault(x => x.Id.Equals(d.Id)));

                // Test that we can use the returned entity set for update
                var p2 = context.EntitySet<IPerson>().Create();
                p2.Name = "Bob";
                context.SaveChanges();
                pid = p2.Id;
            }
            using (var context = CreateEntityContext(storeName))
            {
                var bob  = context.EntitySet<IPerson>().FirstOrDefault(x => x.Id.Equals(pid));
                Assert.NotNull(bob);
                Assert.Equal("Bob", bob.Name);
            }
        }

        [Fact]
        public void TestRetrieveUnsetId()
        {
            MyEntityContext.InitializeEntityMappingStore();
            var entity = new Person();
            var id = entity.Id;
            Assert.Equal(null, id);
        }

        [Fact]
        public void TestRepositoryPattern()
        {
            var storeName = "TestRepositoryPattern" + DateTime.Now.Ticks;
            string id;
            using (var context = CreateEntityContext(storeName))
            {
                var uow = new UnitOfWork(context);
                var repo = new Repository<IDerivedEntity>(uow);
                var derived = repo.Create();
                derived.BaseStringValue = "Party!";
                derived.DateTimeProperty= new DateTime(1999, 12, 31, 23, 58, 00);
                context.SaveChanges();
                id = derived.Id;
            }

            using (var context = CreateEntityContext(storeName))
            {
                var uow = new UnitOfWork(context);
                var repo = new Repository<IDerivedEntity>(uow);
                var derived = repo.GetById(id);
                Assert.NotNull(derived);
                Assert.Equal(id, derived.Id);
                Assert.Equal("Party!", derived.BaseStringValue);
                Assert.Equal(new DateTime(1999, 12, 31, 23, 58, 00), derived.DateTimeProperty);
            }
        }

        [Fact]
        public void TestRetrieveEntityWithSpaceInId()
        {
            var storeName = "TestRetrieveEntityWithSpaceInId_" + DateTime.Now.Ticks;
            using (var context = CreateEntityContext(storeName))
            {
                var entity = new TestEntity {Id = "some entity", SomeString = "Some Entity"};
                context.TestEntities.Add(entity);
                context.SaveChanges();
            }

            using (var context = CreateEntityContext(storeName))
            {
                var entity = context.TestEntities.FirstOrDefault(x => x.Id.Equals("some entity"));
                Assert.NotNull(entity);
            }
        }

        [Theory]
        [InlineData("\\")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r")]
        [InlineData("\b")]
        [InlineData("\f")]
        [InlineData("\"")]
        [InlineData("'")]
        public void TestEscapeOfStringValues(string sep)
        {
            var storeName = "TestBackslashInStringValue_" + DateTime.Now.Ticks;
            var testString = "Client" + sep + "Server";
            using (var context = CreateEntityContext(storeName))
            {
                var entity = new TestEntity {Id="test", SomeString = testString};
                context.TestEntities.Add(entity);
                context.SaveChanges();
            }

            using (var context = CreateEntityContext(storeName))
            {
                var entity = context.TestEntities.FirstOrDefault(x => x.SomeString == testString);
                Assert.NotNull(entity);
                Assert.NotNull(entity.Id);
                Assert.Equal("test", entity.Id);
            }
        }

        [Fact]
        public void TestReattachModifiedEntity()
        {
            var storeName = "TestReattachModifiedEntity_" + DateTime.Now.Ticks;
            ITestEntity entity;
            using (var context = CreateEntityContext(storeName))
            {
                entity = new TestEntity {Id="test", SomeString="Initial Value"};
                context.TestEntities.Add(entity);
                context.SaveChanges();
            }

            using (var context = CreateEntityContext(storeName))
            {
                entity = context.TestEntities.FirstOrDefault(x => x.Id.Equals("test"));
                ((BrightstarEntityObject)entity).Detach();
            }

            using (var context = CreateEntityContext(storeName))
            {
                entity.SomeString = "Updated Value";
                context.Add(entity);
                context.SaveChanges();
            }

            using (var context = CreateEntityContext(storeName))
            {
                entity = context.TestEntities.FirstOrDefault(x => x.Id.Equals("test"));
                Assert.NotNull(entity);
                Assert.NotNull(entity.SomeString);
                Assert.Equal("Updated Value", entity.SomeString);
            }
        }

        MyEntityContext CreateEntityContext(string storeName)
        {
            return new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName);
        }
    }

    public class UnitOfWork
    {
        public MyEntityContext Context { get; private set; }

        public UnitOfWork(MyEntityContext context)
        {
            Context = context;
        }

        public void Save()
        {
            Context.SaveChanges();
        }
    }

    public class Repository<T> where T : class, IBaseEntity
    {
        private readonly UnitOfWork _unitOfWork;

        public Repository(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public T Create()
        {
            return _unitOfWork.Context.EntitySet<T>().Create();
        }

       
        public T GetById(string id)
        {
            return _unitOfWork.Context.EntitySet<T>().FirstOrDefault(x => x.Id == id);
        }
    }
}
