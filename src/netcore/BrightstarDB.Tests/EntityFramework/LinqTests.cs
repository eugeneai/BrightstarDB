using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BrightstarDB.Client;
using Xunit;

namespace BrightstarDB.Tests.EntityFramework
{
    [Collection("BrightstarService")]
    public class LinqTests : IDisposable
    {
        private readonly string _connectionStringTemplate;

        public LinqTests()
        {
            _connectionStringTemplate = "type=embedded;StoresDirectory={1};storeName={2}";
        }

        private string GetConnectionString(string testName)
        {
            return string.Format(_connectionStringTemplate,
                Configuration.DataLocation,
                Configuration.StoreLocation,
                testName + "_" + DateTime.Now.Ticks);
        }

        public void Dispose()
        {
            BrightstarService.Shutdown(false);
        }

        [Fact]
        public void TestLinqCount()
        {
            var connectionString = GetConnectionString("TestLinqCount");
            var context = new MyEntityContext(connectionString);
            for(var i = 0; i<100; i++)
            {
                var entity = context.TestEntities.Create();
                entity.SomeString = "Entity " + i;
                entity.SomeInt = i;
            }
            context.SaveChanges();

            var count = context.TestEntities.Count();

            Assert.NotNull(count);
            Assert.Equal(100, count);

            for (var j = 0; j < 100; j++)
            {
                var entity = context.TestEntities.Create();
                entity.SomeString = "Entity " + j;
                entity.SomeInt = j;
            }
            context.SaveChanges();

            var count2 = context.TestEntities.Count();

            Assert.NotNull(count2);
            Assert.Equal(200, count2);
        }

        [Fact]
        public void TestLinqLongCount()
        {
            var connectionString = GetConnectionString("TestLinqLongCount");
            var context = new MyEntityContext(connectionString);
            for (var i = 0; i < 100; i++)
            {
                var entity = context.TestEntities.Create();
                entity.SomeString = "Entity " + i;
            }
            context.SaveChanges();

            var count = context.TestEntities.LongCount();

            Assert.NotNull(count);
            Assert.Equal(100, count);
        }

        [Fact]
        public void TestLinqAverage()
        {
            var connectionString = GetConnectionString("TestLinqAverage");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            e1.SomeInt = 10;
            e1.SomeDecimal = 10;
            e1.SomeDouble = 10;
            
            var e2 = context.TestEntities.Create();
            e2.SomeInt = 12;
            e2.SomeDecimal = 12;
            e2.SomeDouble = 12;
            
            var e3 = context.TestEntities.Create();
            e3.SomeInt = 15;
            e3.SomeDecimal = 15;
            e3.SomeDouble = 15;
            
            var e4 = context.TestEntities.Create();
            e4.SomeInt = 10;
            e4.SomeDecimal = 10;
            e4.SomeDouble = 10;
            
            var e5 = context.TestEntities.Create();
            e5.SomeInt = 11;
            e5.SomeDecimal = 11;
            e5.SomeDouble = 11;


            context.SaveChanges();
            Assert.Equal(5, context.TestEntities.Count());

            var avInt = context.TestEntities.Average(e => e.SomeInt);
            Assert.NotNull(avInt);
            Assert.Equal(11.6, avInt);

            var avDec = context.TestEntities.Average(e => e.SomeDecimal);
            Assert.NotNull(avDec);
            Assert.Equal(11.6m, avDec);

            var avDbl = context.TestEntities.Average(e => e.SomeDouble);
            Assert.NotNull(avDbl);
            Assert.Equal(11.6, avDbl);

        }

        [Fact]
        public void TestLinqAverage2()
        {
            var connectionString = GetConnectionString("TestLinqAverage2");
            var context = new MyEntityContext(connectionString);
            var ages = new List<int>();
            for (int i = 0; i < 1000; i++)
            {
                var entity = context.TestEntities.Create();
                entity.SomeString = "Person" + i;
                int age = 20 + (i / 20);
                entity.SomeInt = age;
                ages.Add(age);
            }
            context.SaveChanges();

            var total1 = context.TestEntities.Sum(e => e.SomeInt);
            var total2 = ages.Sum();

            var q1 = context.TestEntities.Count();
            var q2 = ages.Count;

            Assert.Equal(total2 / q2, total1 / q1);

            Assert.Equal(1000, context.TestEntities.Count());

            Assert.Equal(ages.Average(), context.TestEntities.Average(e => e.SomeInt));
        }

        [Fact]
        public void TestLinqSum()
        {
            var connectionString = GetConnectionString("TestLinqSum");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            e1.SomeInt = 10;
            e1.SomeDecimal = 10.1m;
            e1.SomeDouble = 10.2;

            var e2 = context.TestEntities.Create();
            e2.SomeInt = 12;
            e2.SomeDecimal = 12.1m;
            e2.SomeDouble = 12.2;

            var e3 = context.TestEntities.Create();
            e3.SomeInt = 15;
            e3.SomeDecimal = 15.1m;
            e3.SomeDouble = 15.2;

            var e4 = context.TestEntities.Create();
            e4.SomeInt = 10;
            e4.SomeDecimal = 10.1m;
            e4.SomeDouble = 10.2;

            var e5 = context.TestEntities.Create();
            e5.SomeInt = 11;
            e5.SomeDecimal = 11.1m;
            e5.SomeDouble = 11.2;


            context.SaveChanges();
            Assert.Equal(5, context.TestEntities.Count());

            var sumInt = context.TestEntities.Sum(e => e.SomeInt);
            Assert.NotNull(sumInt);
            Assert.Equal(58, sumInt);

            var sumDec = context.TestEntities.Sum(e => e.SomeDecimal);
            Assert.NotNull(sumDec);
            Assert.Equal(58.5m, sumDec);

            var sumDbl = context.TestEntities.Sum(e => e.SomeDouble);
            Assert.NotNull(sumDbl);
            Assert.Equal(59.0, sumDbl);

        }
        
        [Fact]
        public void TestLinqContainsString()
        {
            var connectionString = GetConnectionString("TestLinqContainsString");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfStrings = new List<string> {"Jen", "Kal", "Gra", "Andy"};
            var e2 = context.TestEntities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfStrings = new List<string> {"Miranda", "Sadik", "Tobey", "Ian"};
            
            context.SaveChanges();

            Assert.Equal(2, context.TestEntities.Count());
            
            var containsString = context.TestEntities.Where(e => e.CollectionOfStrings.Contains("Jen")).ToList();
            Assert.NotNull(containsString);
            Assert.Equal(1, containsString.Count());
            Assert.Equal("Networked Planet", containsString.First().SomeString);

            var matchTargets = new List<string> {"Samarind", "IBM", "Microsoft"};
            var matchCompanies = context.TestEntities.Where(e => matchTargets.Contains(e.SomeString)).ToList();
            Assert.NotNull(matchCompanies);
            Assert.Equal(1, matchCompanies.Count);
            Assert.Equal("Samarind", matchCompanies.First().SomeString);
        }

        [Fact]
        public void TestLinqContainsInt()
        {
            var connectionString = GetConnectionString("TestLinqContainsInt");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfInts = new List<int>() { 2, 4, 6, 8, 10 };
            var e2 = context.TestEntities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfInts = new List<int>() { 1, 3, 5, 7, 9 };
            
            context.SaveChanges();

            Assert.Equal(2, context.TestEntities.Count());

            var containsInt = context.TestEntities.Where(e => e.CollectionOfInts.Contains(3)).ToList();
            Assert.NotNull(containsInt);
            Assert.Equal(1, containsInt.Count);
            Assert.Equal("Samarind", containsInt.First().SomeString);

        }

        [Fact]
        public void TestLinqContainsDateTime()
        {
            var connectionString = GetConnectionString("TestLinqContainsDateTime");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            var now = DateTime.Now;

            e1.SomeString = "Networked Planet";
            e1.CollectionOfStrings = new List<string> { "Jen", "Kal", "Gra", "Andy" };
            e1.CollectionOfDateTimes = new List<DateTime>() { now.AddYears(2), now.AddYears(4) };
            var e2 = context.TestEntities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfStrings = new List<string> { "Miranda", "Sadik", "Tobey", "Ian" };
            e2.CollectionOfDateTimes = new List<DateTime>() { now.AddYears(1), now.AddYears(3) };
            
            context.SaveChanges();

            Assert.Equal(2, context.TestEntities.Count());

            var containsDateTime =
                context.TestEntities.Where(e => e.CollectionOfDateTimes.Contains(now.AddYears(2))).ToList();
            Assert.NotNull(containsDateTime);
            Assert.Equal(1, containsDateTime.Count);
            Assert.Equal("Networked Planet", containsDateTime.First().SomeString);

        }

        [Fact]
        public void TestLinqContainsDouble()
        {
            var connectionString = GetConnectionString("TestLinqContainsDouble");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfDoubles = new List<double>() { 2.5, 4.5, 6.5, 8.5, 10.5 };
            var e2 = context.TestEntities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfDoubles = new List<double>() { 1.5, 3.5, 5.5, 7.5, 9.5 };

            context.SaveChanges();

            Assert.Equal(2, context.TestEntities.Count());

            var containsDouble = context.TestEntities.Where(e => e.CollectionOfDoubles.Contains(8.5)).ToList();
            Assert.NotNull(containsDouble);
            Assert.Equal(1, containsDouble.Count);
            Assert.Equal("Networked Planet", containsDouble.First().SomeString);

        }

        [Fact]
        public void TestLinqContainsFloat()
        {
            var connectionString = GetConnectionString("TestLinqContainsFloat");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfFloats = new List<float> { 2.5F, 4.5F, 6.5F, 8.5F, 10.5F };
            var e2 = context.TestEntities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfFloats = new List<float> { 1.5F, 3.5F, 5.5F, 7.5F, 9.5F };

            context.SaveChanges();

            Assert.Equal(2, context.TestEntities.Count());

            var containsFloat = context.TestEntities.Where(e => e.CollectionOfFloats.Contains(6.5F)).ToList();
            Assert.NotNull(containsFloat);
            Assert.Equal(1, containsFloat.Count);
            Assert.Equal("Networked Planet", containsFloat.First().SomeString);

        }

        [Fact]
        public void TestLinqContainsDecimal()
        {
            var connectionString = GetConnectionString("TestLinqContainsDecimal");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfDecimals = new List<decimal> { 2.5M, 4.5M, 6.5M, 8.5M, 10.5M };
            var e2 = context.TestEntities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfDecimals = new List<decimal> { 1.5M, 3.5M, 5.5M, 7.5M, 9.5M };

            context.SaveChanges();

            Assert.Equal(2, context.TestEntities.Count());

            var containsDecimal = context.TestEntities.Where(e => e.CollectionOfDecimals.Contains(9.5M)).ToList();
            Assert.NotNull(containsDecimal);
            Assert.Equal(1, containsDecimal.Count);
            Assert.Equal("Samarind", containsDecimal.First().SomeString);

        }

        [Fact]
        public void TestLinqContainsBool()
        {
            var connectionString = GetConnectionString("TestLinqContainsBool");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfBools = new List<bool>() { true };
            var e2 = context.TestEntities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfBools = new List<bool>() { false };

            context.SaveChanges();

            Assert.Equal(2, context.TestEntities.Count());

            var containsBool = context.TestEntities.Where(e => e.CollectionOfBools.Contains(false)).ToList();
            Assert.NotNull(containsBool);
            Assert.Equal(1, containsBool.Count);
            Assert.Equal("Samarind", containsBool.First().SomeString);

        }

        [Fact]
        public void TestLinqContainsLong()
        {
            var connectionString = GetConnectionString("TestLinqContainsLong");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfLong = new List<long>() { 2000000000000, 4000000000000 };
            var e2 = context.TestEntities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfLong = new List<long>() { 3000000000000, 5000000000000 };

            context.SaveChanges();

            Assert.Equal(2, context.TestEntities.Count());

            var containsLong = context.TestEntities.Where(e => e.CollectionOfLong.Contains(2000000000000)).ToList();
            Assert.NotNull(containsLong);
            Assert.Equal(1, containsLong.Count);
            Assert.Equal("Networked Planet", containsLong.First().SomeString);

        }


        [Fact]
        public void TestLinqDistinct()
        {
            var connectionString = GetConnectionString("TestLinqDistinct");
            var context = new MyEntityContext(connectionString);
            
             var entity1 = context.TestEntities.Create();
            entity1.SomeString = "Apples";
            entity1.SomeInt = 2;

            var entity2 = context.TestEntities.Create();
            entity2.SomeString = "Bananas";
            entity2.SomeInt = 2;

            var entity3 = context.TestEntities.Create();
            entity3.SomeString = "Carrots";
            entity3.SomeInt = 8;

            var entity4 = context.TestEntities.Create();
            entity4.SomeString = "Apples";
            entity4.SomeInt = 10;

            var entity5 = context.TestEntities.Create();
            entity5.SomeString = "Apples";
            entity5.SomeInt = 2;

            context.SaveChanges();

            var categories = context.TestEntities.Select(x => x.SomeString).Distinct().ToList();
            Assert.Equal(3, categories.Count());
            Assert.True(categories.Contains("Apples"));
            Assert.True(categories.Contains("Bananas"));
            Assert.True(categories.Contains("Carrots"));

        }

        [Fact]
        public void TestOrderedDistinct()
        {
            var connectionString = GetConnectionString("TestOrderedDistinct");
            var context = new MyEntityContext(connectionString);

            var alice = context.Persons.Create();
            alice.Name = "Alice";
            var bob = context.Persons.Create();
            bob.Name = "Bob";
            var carol = context.Persons.Create();
            carol.Name = "Carol";

            var programming = context.Skills.Create();
            programming.Name = "Programming";
            var csharp = context.Skills.Create();
            csharp.Name = "C#";
            csharp.Parent = programming;
            csharp.SkilledPeople.Add(alice);
            csharp.SkilledPeople.Add(bob);

            var vb = context.Skills.Create();
            vb.Name = "Visual Basic";
            vb.Parent = programming;
            vb.SkilledPeople.Add(alice);
            vb.SkilledPeople.Add(carol);

            var fsharp = context.Skills.Create();
            fsharp.Name = "F#";
            fsharp.Parent = programming;
            fsharp.SkilledPeople.Add(alice);

            context.SaveChanges();

            //var allProgrammers =
            //    context.Skills.Where(x => x.Parent.Id.Equals(programming.Id)).SelectMany(s => s.SkilledPeople).ToList();
            //// No distinct so we will get alice three times
            //Assert.Equal(allProgrammers.Count, 5);
            //Assert.Equal(allProgrammers.Where(p=>p.Name.Equals("alice")), 3);

            var allProgrammersDistinct =
                context.Skills.Where(x => x.Parent.Id.Equals(programming.Id)).SelectMany(s => s.SkilledPeople).Distinct().ToList();
            // Distinct so we will get alice only once
            Assert.Equal(3, allProgrammersDistinct.Count);
            Assert.Equal(1, allProgrammersDistinct.Count(p => p.Name.Equals("Alice")));

            var allProgrammersOrderedDistinct =
                context.Skills.Where(x => x.Parent.Id.Equals(programming.Id)).SelectMany(s => s.SkilledPeople).OrderByDescending(p=>p.Name).Distinct().ToList();
            // Distinct so we will get alice only once
            Assert.Equal(3, allProgrammersOrderedDistinct.Count);
            Assert.Equal("Carol", allProgrammersOrderedDistinct[0].Name);
            Assert.Equal("Bob", allProgrammersOrderedDistinct[1].Name);
            Assert.Equal("Alice", allProgrammersOrderedDistinct[2].Name);
        }

        [Fact]
        public void TestLinqFirst()
        {
            var connectionString = GetConnectionString("TestLinqFirst");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.Equal(6, context.Persons.Count());

            var orderedByName = context.Persons.OrderBy(p => p.Name);
            Assert.NotNull(orderedByName);
            Assert.Equal(6, orderedByName.Count());

            var first = orderedByName.First();
            Assert.NotNull(first);
            Assert.Equal("Annie", first.Name);
        }

        [Fact]
        public void TestLinqFirstOrDefault()
        {
            var connectionString = GetConnectionString("TestLinqFirstOrDefault");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.Equal(6, context.Persons.Count());


            var first = context.Persons.Where(p => p.Name.Equals("Annie")).FirstOrDefault();
            Assert.NotNull(first);
            Assert.Equal("Annie", first.Name);

            var notfound = context.Persons.Where(p => p.Name.Equals("Jo")).FirstOrDefault();
            Assert.Null(notfound);
        }

        [Fact]
        public void TestLinqFirstFail()
        {
            var connectionString = GetConnectionString("TestLinqFirstFail");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.Equal(6, context.Persons.Count());


            var first = context.Persons.Where(p => p.Name.Equals("Annie")).FirstOrDefault();
            Assert.NotNull(first);
            Assert.Equal("Annie", first.Name);

            Assert.Throws<InvalidOperationException>(() =>
            {
                var notfound = context.Persons.Where(p => p.Name.Equals("Jo")).First();
                Assert.Null(notfound);
            });
        }

        [Fact(Skip = "Ignored")]
        public void TestLinqGroupBy()
        {
            var connectionString = GetConnectionString("TestLinqGroupBy");
            var context = new MyEntityContext(connectionString);

            var pe = context.Persons.Create();
            pe.Name = "Bill";
            pe.Age = 51;
            
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.Age = 51;

            var pf = context.Persons.Create();
            pf.Name = "Bill";
            pf.Age = 47;

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.Age = 47;

            var pc = context.Persons.Create();
            pc.Name = "Dennis";
            pc.Age = 20;

            var pa = context.Persons.Create();
            pa.Name = "Dennis";
            pb.Age = 28;

            context.SaveChanges();

            Assert.Equal(6, context.Persons.Count());

            var grpByAge = context.Persons.GroupBy(people => people.Age);
            foreach(var item in grpByAge)
            {
                var age = item.Key;
                var count = item.Count();
            }

            var grpNyName = from p in context.Persons
                           group p by p.Name into g
                           orderby g.Key
                           select new { Name = g.Key, Count = g.Count() };

            foreach (var item in grpNyName)
            {
                var age = item.Name;
                var numInGroup = item.Count;
            }

            var grpByAge2 = from p in context.Persons
                           group p by p.Age into g
                           orderby g.Key
                           select new { Age = g.Key, Count = g.Count() };

            foreach (var item in grpByAge2)
            {
                var age = item.Age;
                var numInGroup = item.Count;
            }
        }
        
        [Fact]
        public void TestLinqMax()
        {
            var connectionString = GetConnectionString("TestLinqMax");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            e1.SomeInt = 10;
            e1.SomeDecimal = 10.21m;
            e1.SomeDouble = 10.21;

            var e2 = context.TestEntities.Create();
            e2.SomeInt = 12;
            e2.SomeDecimal = 12.56m;
            e2.SomeDouble = 12.56;

            var e3 = context.TestEntities.Create();
            e3.SomeInt = 15;
            e3.SomeDecimal = 15.45m;
            e3.SomeDouble = 15.45;

            var e4 = context.TestEntities.Create();
            e4.SomeInt = 9;
            e4.SomeDecimal = 10.11m;
            e4.SomeDouble = 10.11;

            var e5 = context.TestEntities.Create();
            e5.SomeInt = 16;
            e5.SomeDecimal = 15.99m;
            e5.SomeDouble = 15.99;


            context.SaveChanges();
            Assert.Equal(5, context.TestEntities.Count());

            var maxInt = context.TestEntities.Max(e => e.SomeInt);
            Assert.NotNull(maxInt);
            Assert.Equal(16, maxInt);
            var maxDec = context.TestEntities.Max(e => e.SomeDecimal);
            Assert.NotNull(maxDec);
            Assert.Equal(15.99m, maxDec);
            var maxDbl = context.TestEntities.Max(e => e.SomeDouble);
            Assert.NotNull(maxDbl);
            Assert.Equal(15.99, maxDbl);
        }

        [Fact]
        public void TestLinqMin()
        {
            var connectionString = GetConnectionString("TestLinqMin");
            var context = new MyEntityContext(connectionString);

            var e1 = context.TestEntities.Create();
            e1.SomeInt = 10;
            e1.SomeDecimal = 10.21m;
            e1.SomeDouble = 10.21;

            var e2 = context.TestEntities.Create();
            e2.SomeInt = 12;
            e2.SomeDecimal = 12.56m;
            e2.SomeDouble = 12.56;

            var e3 = context.TestEntities.Create();
            e3.SomeInt = 15;
            e3.SomeDecimal = 15.45m;
            e3.SomeDouble = 15.45;

            var e4 = context.TestEntities.Create();
            e4.SomeInt = 9;
            e4.SomeDecimal = 10.11m;
            e4.SomeDouble = 10.11;

            var e5 = context.TestEntities.Create();
            e5.SomeInt = 16;
            e5.SomeDecimal = 15.99m;
            e5.SomeDouble = 15.99;


            context.SaveChanges();
            Assert.Equal(5, context.TestEntities.Count());

            var minInt = context.TestEntities.Min(e => e.SomeInt);
            Assert.NotNull(minInt);
            Assert.Equal(9, minInt);
            var minDec = context.TestEntities.Min(e => e.SomeDecimal);
            Assert.NotNull(minDec);
            Assert.Equal(10.11m, minDec);
            var minDbl = context.TestEntities.Min(e => e.SomeDouble);
            Assert.NotNull(minDbl);
            Assert.Equal(10.11, minDbl);
        }

        [Fact]
        public void TestLinqOrderByString()
        {
            var connectionString = GetConnectionString("TestLinqOrderByString");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.Equal(6, context.Persons.Count());

            var orderedByName = context.Persons.OrderBy(p => p.Name);
            Assert.NotNull(orderedByName);
            Assert.Equal(6, orderedByName.Count());
            var i = 0;
            foreach (var p in orderedByName)
            {
                Assert.NotNull(p.Name);
                switch (i)
                {
                    case 0:
                        Assert.Equal("Annie", p.Name);
                        break;
                    case 1:
                        Assert.Equal("Bill", p.Name);
                        break;
                    case 2:
                        Assert.Equal("Carole", p.Name);
                        break;
                    case 3:
                        Assert.Equal("Dennis", p.Name);
                        break;
                    case 4:
                        Assert.Equal("Eddie", p.Name);
                        break;
                    case 5:
                        Assert.Equal("Freddie", p.Name);
                        break;
                }
                i++;
            }

            var orderedByNameDesc = context.Persons.OrderByDescending(p => p.Name);
            Assert.NotNull(orderedByNameDesc);
            Assert.Equal(6, orderedByNameDesc.Count());
            var j = 0;
            foreach (var p in orderedByNameDesc)
            {
                Assert.NotNull(p.Name);
                switch (j)
                {
                    case 5:
                        Assert.Equal("Annie", p.Name);
                        break;
                    case 4:
                        Assert.Equal("Bill", p.Name);
                        break;
                    case 3:
                        Assert.Equal("Carole", p.Name);
                        break;
                    case 2:
                        Assert.Equal("Dennis", p.Name);
                        break;
                    case 1:
                        Assert.Equal("Eddie", p.Name);
                        break;
                    case 0:
                        Assert.Equal("Freddie", p.Name);
                        break;
                }
                j++;
            }
        }

        [Fact]
        public void TestLinqOrderByDate()
        {
            var connectionString = GetConnectionString("TestLinqOrderByDate");
            var context = new MyEntityContext(connectionString);
            
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            pe.DateOfBirth = new DateTime(1969, 8, 8, 4, 5, 30);

            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.DateOfBirth = new DateTime(1900, 1, 12);
            
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            pf.DateOfBirth = new DateTime(1969, 8, 8, 4, 6, 30);

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.DateOfBirth = new DateTime(1962, 4, 20);

            var pc = context.Persons.Create();
            pc.Name = "Carole";
            pc.DateOfBirth = new DateTime(1962, 3, 11);
            
            var pa = context.Persons.Create();
            pa.Name = "Annie";
            pa.DateOfBirth = new DateTime(1950, 2, 2);

            context.SaveChanges();
            Assert.Equal(6, context.Persons.Count());

            var orderedByDob = context.Persons.OrderBy(p => p.DateOfBirth);
            Assert.NotNull(orderedByDob);
            Assert.Equal(6, orderedByDob.Count());
            var i = 0;
            foreach (var p in orderedByDob)
            {
                Assert.NotNull(p.Name);
                Assert.NotNull(p.DateOfBirth);
                switch (i)
                {
                    case 0:
                        Assert.Equal("Bill", p.Name);
                        break;
                    case 1:
                        Assert.Equal("Annie", p.Name);
                        break;
                    case 2:
                        Assert.Equal("Carole", p.Name);
                        break;
                    case 3:
                        Assert.Equal("Dennis", p.Name);
                        break;
                    case 4:
                        Assert.Equal("Eddie", p.Name);
                        break;
                    case 5:
                        Assert.Equal("Freddie", p.Name);
                        break;
                }
                i++;
            }

            var orderedByDobDesc = context.Persons.OrderByDescending(p => p.DateOfBirth);
            Assert.NotNull(orderedByDobDesc);
            Assert.Equal(6, orderedByDobDesc.Count());
            var j = 0;
            foreach (var p in orderedByDobDesc)
            {
                Assert.NotNull(p.Name);
                Assert.NotNull(p.DateOfBirth);
                switch (j)
                {
                    case 5:
                        Assert.Equal("Bill", p.Name);
                        break;
                    case 4:
                        Assert.Equal("Annie", p.Name);
                        break;
                    case 3:
                        Assert.Equal("Carole", p.Name);
                        break;
                    case 2:
                        Assert.Equal("Dennis", p.Name);
                        break;
                    case 1:
                        Assert.Equal("Eddie", p.Name);
                        break;
                    case 0:
                        Assert.Equal("Freddie", p.Name);
                        break;
                }
                j++;
            }
        }

        [Fact]
        public void TestLinqOrderByInteger()
        {
            var connectionString = GetConnectionString("TestLinqOrderByInteger");
            var context = new MyEntityContext(connectionString);

            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            pe.Age = 51;

            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.Age = 111;

            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            pf.Age = 47;

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.Age = 32;

            var pc = context.Persons.Create();
            pc.Name = "Carole";
            pc.Age = 18;

            var pa = context.Persons.Create();
            pa.Name = "Annie";
            pa.Age = 28;

            context.SaveChanges();

            Assert.Equal(6, context.Persons.Count());

            var orderedByAge = context.Persons.OrderBy(p => p.Age);
            Assert.NotNull(orderedByAge);
            Assert.Equal(6, orderedByAge.Count());
            var i = 0;
            foreach (var p in orderedByAge)
            {
                Assert.NotNull(p.Name);
                Assert.NotNull(p.Age);
                switch (i)
                {
                    case 0:
                        Assert.Equal("Carole", p.Name);
                        break;
                    case 1:
                        Assert.Equal("Annie", p.Name);
                        break;
                    case 2:
                        Assert.Equal("Dennis", p.Name);
                        break;
                    case 3:
                        Assert.Equal("Freddie", p.Name);
                        break;
                    case 4:
                        Assert.Equal("Eddie", p.Name);
                        break;
                    case 5:
                        Assert.Equal("Bill", p.Name);
                        break;
                }
                i++;
            }

            var orderedByAgeDesc = context.Persons.OrderByDescending(p => p.Age);
            Assert.NotNull(orderedByAgeDesc);
            Assert.Equal(6, orderedByAgeDesc.Count());
            var j = 0;
            foreach (var p in orderedByAgeDesc)
            {
                Assert.NotNull(p.Name);
                Assert.NotNull(p.Age);
                switch (j)
                {
                    case 5:
                        Assert.Equal("Carole", p.Name);
                        break;
                    case 4:
                        Assert.Equal("Annie", p.Name);
                        break;
                    case 3:
                        Assert.Equal("Dennis", p.Name);
                        break;
                    case 2:
                        Assert.Equal("Freddie", p.Name);
                        break;
                    case 1:
                        Assert.Equal("Eddie", p.Name);
                        break;
                    case 0:
                        Assert.Equal("Bill", p.Name);
                        break;
                }
                j++;
            }
            
        }

        [Fact]
        //note - not sure if this is an adequate test of Select()
        public void TestLinqSelect()
        {
            var connectionString = GetConnectionString("TestLinqSelect");
            var context = new MyEntityContext(connectionString);

            for (var i = 1; i < 11; i++ )
            {
                var entity = context.TestEntities.Create();
                entity.SomeInt = i;
            }
            context.SaveChanges();
            Assert.Equal(10, context.TestEntities.Count());

            var select = context.TestEntities.Select(e => e);
            Assert.NotNull(select);
            Assert.Equal(10, select.Count());
        }

        [Fact]
        public void TestLinqSelectMany()
        {
            var connectionString = GetConnectionString("TestLinqSelectMany");
            var context = new MyEntityContext(connectionString);
            
            var skill1 = context.Skills.Create();
            skill1.Name = "C#";
            var skill2 = context.Skills.Create();
            skill2.Name = "HTML";
            var p1 = context.Persons.Create();
            p1.Name = "Jane";
            p1.Skills.Add(skill1);
            p1.Skills.Add(skill2);

            var skill3 = context.Skills.Create();
            skill3.Name = "SQL";
            var skill4 = context.Skills.Create();
            skill4.Name = "NoSQL";
            var p2 = context.Persons.Create();
            p2.Name = "Bob";
            p2.Skills.Add(skill3);
            p2.Skills.Add(skill4);

            var skill5 = context.Skills.Create();
            skill5.Name = "Graphics";
            var p3 = context.Persons.Create();
            p3.Name = "Jez";
            p3.Skills.Add(skill5);

            var skill6 = context.Skills.Create();
            skill6.Name = "CSS";
            
            context.SaveChanges();

            Assert.Equal(3, context.Persons.Count());
            Assert.Equal(6, context.Skills.Count());

            var daskillz = context.Persons.SelectMany(owners => owners.Skills);
            var i = 0;
            foreach(var s in daskillz)
            {
                i++;
                Assert.NotNull(s.Name);
            }
            Assert.Equal(5, i);
            Assert.Equal(5, daskillz.Count());
        }

        [Fact]
        public void TestLinqSingle()
        {
            var connectionString = GetConnectionString("TestLinqSingle");
            var context = new MyEntityContext(connectionString);
            
            var entity = context.TestEntities.Create();
            entity.SomeString = "An entity";
            context.SaveChanges();
            Assert.Equal(1, context.TestEntities.Count());

            var single = context.TestEntities.Single();
            Assert.NotNull(single);
            Assert.Equal("An entity", single.SomeString);
        }

        [Fact]
        public void TestLinqSingleFail()
        {
            var connectionString = GetConnectionString("TestLinqSingleFail");
            var context = new MyEntityContext(connectionString);
            Assert.Throws<InvalidOperationException>(() =>
            {
                var sod = context.TestEntities.Single();
            });
        }

        [Fact]
        public void TestLinqSingleFail2()
        {
            var connectionString = GetConnectionString("TestLinqSingleFail2");
            var context = new MyEntityContext(connectionString);

            for (var i = 1; i < 11; i++)
            {
                var entity = context.TestEntities.Create();
                entity.SomeInt = i;
            }
            context.SaveChanges();
            Assert.Equal(10, context.TestEntities.Count());

            Assert.Throws<InvalidOperationException>(() =>
            {
                var singleFail = context.TestEntities.Single();
            });
        }

        [Fact]
        public void TestLinqSingleOrDefault()
        {
            var connectionString = GetConnectionString("TestLinqSingleOrDefault");
            var context = new MyEntityContext(connectionString);

            var sod = context.TestEntities.SingleOrDefault();
            Assert.Null(sod);

            var entity = context.TestEntities.Create();
            entity.SomeString = "An entity";
            context.SaveChanges();
            Assert.Equal(1, context.TestEntities.Count());

            var single = context.TestEntities.SingleOrDefault();
            Assert.NotNull(single);
            Assert.Equal("An entity", single.SomeString);

            for (var i = 1; i < 10; i++)
            {
                var e = context.TestEntities.Create();
                e.SomeInt = i;
            }
            context.SaveChanges();
            Assert.Equal(10, context.TestEntities.Count());

            //var sod = context.Entities.SingleOrDefault();
            //Assert.Null(sod);

        }

        [Fact]
        public void TestLinqSingleOrDefaultFail()
        {
            var connectionString = GetConnectionString("TestLinqSingleOrDefaultFail");
            var context = new MyEntityContext(connectionString);

            var sod = context.TestEntities.SingleOrDefault();
            Assert.Null(sod);

            for (var i = 0; i < 10; i++)
            {
                var e = context.TestEntities.Create();
                e.SomeInt = i;
            }
            context.SaveChanges();
            Assert.Equal(10, context.TestEntities.Count());

            Assert.Throws<InvalidOperationException>(() =>
            {
                var sod2 = context.TestEntities.SingleOrDefault();
            });
        }

        [Fact]
        public void TestLinqSkip()
        {
            var connectionString = GetConnectionString("TestLinqSkip");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.Equal(6, context.Persons.Count());

            var orderedByName = context.Persons.OrderBy(p => p.Name).Skip(2);
            Assert.NotNull(orderedByName);
            Assert.Equal(4, orderedByName.ToList().Count());
            var i = 0;
            foreach (var p in orderedByName)
            {
                Assert.NotNull(p.Name);
                switch (i)
                {
                    case 0:
                        Assert.Equal("Carole", p.Name);
                        break;
                    case 1:
                        Assert.Equal("Dennis", p.Name);
                        break;
                    case 2:
                        Assert.Equal("Eddie", p.Name);
                        break;
                    case 3:
                        Assert.Equal("Freddie", p.Name);
                        break;
                }
                i++;
            }
            Assert.Equal(4, i);
        }

        [Fact]
        public void TestLinqTake()
        {
            var connectionString = GetConnectionString("TestLinqTake");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.Equal(6, context.Persons.Count());

            var orderedByName = context.Persons.OrderBy(p => p.Name).Skip(3).Take(2);
            Assert.NotNull(orderedByName);
            //Assert.Equal(2, orderedByName.Count());
            var i = 0;
            foreach (var p in orderedByName)
            {
                Assert.NotNull(p.Name);
                switch (i)
                {
                    case 0:
                        Assert.Equal("Dennis", p.Name);
                        break;
                    case 1:
                        Assert.Equal("Eddie", p.Name);
                        break;
                }
                i++;
            }
            Assert.Equal(2, i);
        }

        [Fact]
        public void TestLinqThenBy()
        {
            var connectionString = GetConnectionString("TestLinqThenBy");
            var context = new MyEntityContext(connectionString);

            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            pe.Age = 30;

            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.Age = 30;

            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            pf.Age = 30;

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.Age = 29;

            var pc = context.Persons.Create();
            pc.Name = "Carole";
            pc.Age = 29;

            var pa = context.Persons.Create();
            pa.Name = "Annie";
            pa.Age = 35;

            context.SaveChanges();
            Assert.Equal(6, context.Persons.Count());

            var orderedByAgeThenName = context.Persons.OrderBy(p => p.Age).ThenBy(p => p.Name);
            Assert.NotNull(orderedByAgeThenName);
            Assert.Equal(6, orderedByAgeThenName.Count());
            var i = 0;
            foreach (var p in orderedByAgeThenName)
            {
                Assert.NotNull(p.Name);
                Assert.NotNull(p.Age);
                switch (i)
                {
                    case 0:
                        Assert.Equal("Carole", p.Name);
                        break;
                    case 1:
                        Assert.Equal("Dennis", p.Name);
                        break;
                    case 2:
                        Assert.Equal("Bill", p.Name);
                        break;
                    case 3:
                        Assert.Equal("Eddie", p.Name);
                        break;
                    case 4:
                        Assert.Equal("Freddie", p.Name);
                        break;
                    case 5:
                        Assert.Equal("Annie", p.Name);
                        break;
                }
                i++;
            }

        }

        [Fact]
        public void TestLinqThenByDescending()
        {
            var connectionString = GetConnectionString("TestLinqThenByDescending");
            var context = new MyEntityContext(connectionString);

            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            pe.Age = 30;

            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.Age = 30;

            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            pf.Age = 30;

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.Age = 29;

            var pc = context.Persons.Create();
            pc.Name = "Carole";
            pc.Age = 29;

            var pa = context.Persons.Create();
            pa.Name = "Annie";
            pa.Age = 35;

            context.SaveChanges();
            Assert.Equal(6, context.Persons.Count());

            var orderedByAgeThenName = context.Persons.OrderBy(p => p.Age).ThenByDescending(p => p.Name);
            Assert.NotNull(orderedByAgeThenName);
            Assert.Equal(6, orderedByAgeThenName.Count());
            var i = 0;
            foreach (var p in orderedByAgeThenName)
            {
                Assert.NotNull(p.Name);
                Assert.NotNull(p.Age);
                switch (i)
                {
                    case 0:
                        Assert.Equal("Dennis", p.Name);
                        break;
                    case 1:
                        Assert.Equal("Carole", p.Name);
                        break;
                    case 2:
                        Assert.Equal("Freddie", p.Name);
                        break;
                    case 3:
                        Assert.Equal("Eddie", p.Name);
                        break;
                    case 4:
                        Assert.Equal("Bill", p.Name);
                        break;
                    case 5:
                        Assert.Equal("Annie", p.Name);
                        break;
                }
                i++;
            }
        }

        [Fact]
        public void TestLinqWhere()
        {
            var connectionString = GetConnectionString("TestLinqWhere");
            var context = new MyEntityContext(connectionString);

            // Setup
            var programming = context.Skills.Create();
            programming.Name = "Programming";
            var projectManagement = context.Skills.Create();
            projectManagement.Name = "Project Management";
            var graphicDesign = context.Skills.Create();
            graphicDesign.Name = "Graphic Design";

            var pe = context.Persons.Create();
            pe.Name = "Alex";
            pe.Age = 30;
            pe.MainSkill = programming;

            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.Age = 30;
            pb.MainSkill = projectManagement;

            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            pf.Age = 30;
            pf.MainSkill = graphicDesign;

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.Age = 29;
            pd.Friends.Add(pe);
            pd.MainSkill = programming;

            var pc = context.Persons.Create();
            pc.Name = "Carole";
            pc.Age = 29;
            pc.MainSkill = projectManagement;

            var pa = context.Persons.Create();
            pa.Name = "Annie";
            pa.Age = 35;
            pa.MainSkill = graphicDesign;

            context.SaveChanges();

            // Assert
            Assert.Equal(6, context.Persons.Count());

            var age30 = context.Persons.Where(p => p.Age.Equals(30));
            Assert.Equal(3, age30.Count());
            var older = context.Persons.Where(p => p.Age > 30);
            Assert.Equal(1, older.Count());
            var younger = context.Persons.Where(p => p.Age < 30);
            Assert.Equal(2, younger.Count());

            var startswithA = context.Persons.Where(p => p.Name.StartsWith("A"));
            Assert.Equal(2, startswithA.Count());

            var endsWithE = context.Persons.Where(p => p.Name.EndsWith("e"));
            Assert.Equal(3, endsWithE.Count());

#if !NETCORE
            endsWithE = context.Persons.Where(p => p.Name.EndsWith("E", true, CultureInfo.CurrentUICulture));
            Assert.Equal(3, endsWithE.Count());

            endsWithE = context.Persons.Where(p => p.Name.EndsWith("E", false, CultureInfo.CurrentUICulture));
            Assert.Equal(0, endsWithE.Count());
#endif

            endsWithE = context.Persons.Where(p => p.Name.EndsWith("E", StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(3, endsWithE.Count());

            endsWithE = context.Persons.Where(p => p.Name.EndsWith("E", StringComparison.CurrentCulture));
            Assert.Equal(0, endsWithE.Count());

            var containsNi = context.Persons.Where(p => p.Name.Contains("ni"));
            Assert.Equal(2, containsNi.Count());

            var x = context.Persons.Where(p => Regex.IsMatch(p.Name, "^a.*e$", RegexOptions.IgnoreCase));
            Assert.Equal(1, x.Count());
            Assert.Equal("Annie", x.First().Name);

            var annie = context.Persons.Where(p => p.Name.Equals("Annie")).SingleOrDefault();
            Assert.NotNull(annie);

            var mainSkillsOfPeopleOver30 = from s in context.Skills where s.Expert.Age > 30 select s;
            var results = mainSkillsOfPeopleOver30.ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("Graphic Design", results.First().Name);

            //note - startswith and getchar are not supported

            //note null is not supported
            //not Count() is not supported
            //var hasFriends = context.Persons.Where(p => p.Friends.Count() > 0);
            //Assert.Equal(1, hasFriends.Count());

            //note length is not supported
            //var longNames = context.Persons.Where(p => p.Name.Length > 6);
            //Assert.Equal(1, longNames.Count());


        }

        [Fact]
        public void TestLinqRelatedWhere()
        {
            var connectionString = GetConnectionString("TestLinqRelatedWhere");
            var context = new MyEntityContext(connectionString);

            // Setup
            for (var i = 0; i < 10; i++)
            {
                var p = context.Persons.Create();
                p.Name = "Person" + i;
                var age = (i + 1)*10;
                p.Age = age;
                var s = context.Skills.Create();
                s.Name = "Skill" +i;
                s.Expert = p;
            }
            context.SaveChanges();

            // Assert
            Assert.Equal(10, context.Persons.Count());
            Assert.Equal(10, context.Skills.Count());

            var skills = from s in context.Skills select s;
            Assert.Equal(10, skills.Count());

            var peopleOver30 = context.Persons.Where(p => p.Age > 30);
            Assert.Equal(7, peopleOver30.Count());

            peopleOver30 = from p in context.Persons where p.Age > 30 select p;
            Assert.Equal(7, peopleOver30.Count());


            var mainSkillsOfPeopleOver30 = from s in context.Skills where s.Expert.Age > 30 select s;
            Assert.Equal(7, mainSkillsOfPeopleOver30.Count());


        }

        [Fact]
        public void TestLinqQuery1()
        {
            var connectionString = GetConnectionString("TestLinqQuery1");
            var context = new MyEntityContext(connectionString);

            var jr1 = context.JobRoles.Create();
            jr1.Description = "development";

            var jr2 = context.JobRoles.Create();
            jr2.Description = "sales";

            var jr3 = context.JobRoles.Create();
            jr3.Description = "marketing";

            var jr4 = context.JobRoles.Create();
            jr4.Description = "management";

            var jr5 = context.JobRoles.Create();
            jr5.Description = "administration";

            context.SaveChanges();

            var roles = new IJobRole[] {jr1, jr2, jr3, jr4, jr5};

            for (var i = 0; i < 100; i++)
            {
                var p = context.Persons.Create();
                p.Name = "Person" + i;
                p.EmployeeId = i;
                p.JobRole = roles[i%5];
            }

            context.SaveChanges();

            // Assert
            Assert.Equal(100, context.Persons.Count());
            Assert.Equal(5, context.JobRoles.Count());

            var management = context.JobRoles.Where(s => s.Description.Equals("management")).First();
            Assert.NotNull(management);
            var managers = management.Persons;
            Assert.NotNull(managers);
            Assert.Equal(20, managers.Count);
        }


        [Fact]
        public void TestLinqJoin1()
        {
            var connectionString = GetConnectionString("TestLinqJoin1");
            var context = new MyEntityContext(connectionString);

            for(var i = 0; i<3; i++)
            {
                var jobrole = context.JobRoles.Create();
                jobrole.Description = "JobRole " + i;
                if (i <= 0) continue;
                for (var j = 0; j < 50; j++)
                {
                    var person = context.Persons.Create();
                    person.Name = "Person " + j;
                    jobrole.Persons.Add(person);
                }
            }
            context.SaveChanges();

            Assert.Equal(3, context.JobRoles.Count());
            Assert.Equal(100, context.Persons.Count());

            var rolesThatHavePeople = (from jobrole in context.JobRoles
                                  join person in context.Persons on jobrole.Id equals person.JobRole.Id
                                  select jobrole).Distinct().ToList();
            Assert.Equal(2, rolesThatHavePeople.Count);
        }

        [Fact]
        public void TestLinqJoinOnProperty()
        {
            var connectionString = GetConnectionString("TestLinqJoinOnProperty");
            var context = new MyEntityContext(connectionString);

            var people = new List<IPerson>();
            for (var i = 0; i < 100; i++)
            {
                var person = context.Persons.Create();
                person.Name = "Person " + i;
                person.EmployeeId = i;
                people.Add(person);
            }

            for (var i = 0; i < 100; i++)
            {
                var article = context.Articles.Create();
                article.Title = "Article " + i;

                var publisher = people.Where(p => p.EmployeeId == i).SingleOrDefault();
                Assert.NotNull(publisher);

                article.Publisher = publisher;
            }
            context.SaveChanges();

            Assert.Equal(100, context.Persons.Count());
            Assert.Equal(100, context.Articles.Count());

            var allArticlesWithPublishers = (from article in context.Articles
                                             join person in context.Persons on article.Publisher.EmployeeId equals
                                                 person.EmployeeId
                                             select article).ToList();
            Assert.Equal(100, allArticlesWithPublishers.Count);


            var allPublishersWithArticles = (from person in context.Persons
                                 join article in context.Articles on person.EmployeeId equals
                                     article.Publisher.EmployeeId
                                 select person).ToList();
            Assert.Equal(100, allPublishersWithArticles.Count);
        }

        [Fact]
        public void TestLinqJoinOnId()
        {
            var connectionString = GetConnectionString("TestLinqJoinOnId");
            var context = new MyEntityContext(connectionString);

            var people = new List<IPerson>();
            for (var i = 0; i < 100; i++)
            {
                var person = context.Persons.Create();
                person.Name = "Person " + i;
                person.EmployeeId = i;
                people.Add(person);
            }

            for (var i = 0; i < 100; i++)
            {
                var article = context.Articles.Create();
                article.Title = "Article " + i;

                var publisher = people.Where(p => p.EmployeeId == i).SingleOrDefault();
                Assert.NotNull(publisher);

                article.Publisher = publisher;
            }
            context.SaveChanges();

            Assert.Equal(100, context.Persons.Count());
            Assert.Equal(100, context.Articles.Count());

            var test = context.Articles.Count(a => a.Publisher != null);
            Assert.Equal(100, test);

            var allArticlesWithPublishers = (from article in context.Articles
                                             join person in context.Persons on article.Publisher.Id equals
                                                 person.Id
                                             select article).ToList();
            Assert.Equal(100, allArticlesWithPublishers.Count);


            var allPublishersWithArticles = (from person in context.Persons
                                             join article in context.Articles on person.Id equals
                                                 article.Publisher.Id
                                             select person).ToList();
            Assert.Equal(100, allPublishersWithArticles.Count);
        }


        [Fact]
        public void TestLinqJoinOnId2()
        {
            var connectionString = GetConnectionString("TestLinqJoinOnId2");
            var context = new MyEntityContext(connectionString);

            var people = new List<IPerson>();
            for (var i = 0; i < 11; i++)
            {
                var person = context.Persons.Create();
                person.Name = "Person " + i;
                person.EmployeeId = i;
                people.Add(person);
            }

            for (var i = 0; i < 100; i++)
            {
                var article = context.Articles.Create();
                article.Title = "Article " + i;

                var publisher = people.Where(p => p.EmployeeId == (i/10)).SingleOrDefault();
                Assert.NotNull(publisher);

                article.Publisher = publisher;
            }
            context.SaveChanges();

            Assert.Equal(11, context.Persons.Count());
            Assert.Equal(100, context.Articles.Count());


            var allArticlesWithPublishers = (from article in context.Articles
                                             join person in context.Persons on article.Publisher.Id equals
                                                 person.Id
                                             select article).ToList();
            Assert.Equal(100, allArticlesWithPublishers.Count);


            var allPublishersWithArticles = (from person in context.Persons
                                             join article in context.Articles on person.Id equals
                                                 article.Publisher.Id
                                             select person).Distinct().ToList();
            Assert.Equal(10, allPublishersWithArticles.Count);
        }

        [Fact]
        public void TestLinqJoinWithFilter()
        {
            var connectionString = GetConnectionString("TestLinqJoinWithFilter");
            var context = new MyEntityContext(connectionString);

            // Setup
            var people = new List<IPerson>();
            for (var i = 0; i < 10; i++)
            {
                var person = context.Persons.Create();
                person.Name = "Person " + i;
                person.EmployeeId = i;
                var age = (i + 2) * 10;
                person.Age = age;
                people.Add(person);
            }

            for (var i = 0; i < 100; i++)
            {
                var article = context.Articles.Create();
                article.Title = "Article " + i;

                var publisher = people.Where(p => p.EmployeeId == (i / 10)).SingleOrDefault();
                Assert.NotNull(publisher);

                article.Publisher = publisher;
            }
            context.SaveChanges();

            // Assert
            Assert.Equal(10, context.Persons.Count());
            Assert.Equal(100, context.Articles.Count());

            var articlesByOldPeopleCount = Enumerable.Count(context.Articles, a => a.Publisher.Age > 50);
            Assert.Equal(60, articlesByOldPeopleCount);

            var articlesByOldPeople = (from person in context.Persons
                                       join article in context.Articles on person.Id equals article.Publisher.Id
                                       where person.Age > 50
                                       select article).ToList();

            Assert.Equal(60, articlesByOldPeople.Count);
        }

        [Fact]
        public void TestLinqRelatedCount()
        {
            var connectionString = GetConnectionString("TestLinqRelatedCount");
            var context = new MyEntityContext(connectionString);

            // Setup
            var people = new List<IPerson>();
            for (var i = 0; i < 11; i++)
            {
                var person = context.Persons.Create();
                person.Name = "Person " + i;
                person.EmployeeId = i;
                var age = (i + 2) * 10;
                person.Age = age;
                people.Add(person);
            }

            for (var i = 0; i < 100; i++)
            {
                var article = context.Articles.Create();
                article.Title = "Article " + i;

                var publisher = people.Where(p => p.EmployeeId == (i / 10)).SingleOrDefault();
                Assert.NotNull(publisher);

                article.Publisher = publisher;
            }
            context.SaveChanges();

            // Assert
            Assert.Equal(11, context.Persons.Count());
            Assert.Equal(100, context.Articles.Count());

            var publishers = context.Articles.Select(a => a.Publisher).Distinct().ToList();
            Assert.Equal(10, publishers.Count);
        }


        [Fact]
       public void TestLinqAny()
        {
            var connectionString = GetConnectionString("TestLinqAny");
           var context = new MyEntityContext(connectionString);
           var deptA = context.Departments.Create();
           deptA.Name = "Department A";
           var deptB = context.Departments.Create();
           deptB.Name = "Department B";
           var alice = context.Persons.Create();
           alice.Age = 25;
           var bob = context.Persons.Create();
           bob.Age = 29;
           var charlie = context.Persons.Create();
           charlie.Age = 21;
           var dave = context.Persons.Create();
           dave.Age = 35;
           deptA.Persons.Add(alice);
           deptA.Persons.Add(bob);
           deptB.Persons.Add(charlie);
           deptB.Persons.Add(dave);
           context.SaveChanges();

           var departmentsWithOldies = context.Departments.Where(d => d.Persons.Any(p => p.Age > 30)).ToList();
           Assert.Equal(1, departmentsWithOldies.Count);
           Assert.Equal(deptB.Id, departmentsWithOldies[0].Id);
       }

        [Fact]
        public void TestLinqAll()
        {
            var connectionString = GetConnectionString("TestLinqAll");
            //if (connectionString.Contains("dotnetrdf"))
            //{
            //    Assert.Inconclusive("Test known to fail due to bug in current build of DotNetRDF.");
            //}
            var context = new MyEntityContext(connectionString);
            var alice = context.Persons.Create();
            alice.Name = "Alice";
            alice.Age = 18;
            var bob = context.Persons.Create();
            bob.Name = "Bob";
            bob.Age = 20;
            var carol = context.Persons.Create();
            carol.Age = 20;
            carol.Name = "Carol";
            var dave = context.Persons.Create();
            dave.Age = 22;
            dave.Name = "Dave";
            var edith = context.Persons.Create();
            edith.Age = 21;
            edith.Name = "Edith";
            alice.Friends.Add(bob);
            alice.Friends.Add(carol);
            bob.Friends.Add(alice);
            bob.Friends.Add(carol);
            carol.Friends.Add(alice);
            carol.Friends.Add(bob);
            carol.Friends.Add(dave);
            dave.Friends.Add(edith);
            context.SaveChanges();

            var results = context.Persons.Where(p => p.Friends.All(f => f.Age < 21)).Select(f=>f.Name).ToList();
            Assert.Equal(3, results.Count);
            Assert.True(results.Contains("Alice"));
            Assert.True(results.Contains("Bob"));
            Assert.True(results.Contains("Edith"));
        }
        
        [Fact]
        public void TestLinqQueryEnum()
        {
            var connectionString = GetConnectionString("TestLinqQueryEnum");
            //if (connectionString.Contains("type=dotnetrdf"))
            //{
            //    Assert.Inconclusive("Enum tests fail against DNR store");
            //}
            var context = new MyEntityContext(connectionString);
            var entity1 = context.TestEntities.Create();
            entity1.SomeEnumeration = TestEnumeration.Second;
            entity1.SomeNullableEnumeration = TestEnumeration.Third;
            entity1.SomeNullableFlagsEnumeration = TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagB;
            context.SaveChanges();
            
            // Find by single flag
            IList<ITestEntity> results = context.TestEntities.Where(e => e.SomeEnumeration == TestEnumeration.Second).ToList();
            Assert.Equal(1, results.Count);
            Assert.True(results.Any(x=>x.Id.Equals(entity1.Id)));


            // Find by flag combo
            results =
                context.TestEntities.Where(
                    e => e.SomeNullableFlagsEnumeration == (TestFlagsEnumeration.FlagB | TestFlagsEnumeration.FlagA)).ToList();
            Assert.Equal(1, results.Count);
            Assert.True(results.Any(x => x.Id.Equals(entity1.Id)));

            // Find by one flag of combo
            results = context.TestEntities.Where(
                e => ((e.SomeNullableFlagsEnumeration & TestFlagsEnumeration.FlagB) == TestFlagsEnumeration.FlagB)).
                ToList();
            Assert.Equal(1, results.Count);
            Assert.True(results.Any(x => x.Id.Equals(entity1.Id)));

            // Find by one flag not set on combo
            results = context.TestEntities.Where(
                e => ((e.SomeNullableFlagsEnumeration & TestFlagsEnumeration.FlagC) == TestFlagsEnumeration.NoFlags)).
                ToList();
            Assert.Equal(1, results.Count);
            Assert.True(results.Any(x => x.Id.Equals(entity1.Id)));

            // Find by both flags set on combo
            results = context.TestEntities.Where(
                e =>
                ((e.SomeNullableFlagsEnumeration & (TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagB)) ==
                 (TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagB))).ToList();
            Assert.Equal(1, results.Count);
            Assert.True(results.Any(x => x.Id.Equals(entity1.Id)));

            results = context.TestEntities.Where(
                e =>
                ((e.SomeNullableFlagsEnumeration & (TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagC)) ==
                 (TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagC))).ToList();
            Assert.Equal(0, results.Count);
            

            // Find by NoFlags
            results =
                context.TestEntities.Where(
                    e => e.SomeFlagsEnumeration == TestFlagsEnumeration.NoFlags).ToList();
            Assert.Equal(1, results.Count);
            Assert.True(results.Any(x => x.Id.Equals(entity1.Id)));
        }

        [Fact]
        public void TestLinqNullComparison()
        {
            var connectionString = GetConnectionString("TestLinqNullComparison");
            var context = new MyEntityContext(connectionString);
            var alice = context.Persons.Create();
            alice.Name = "Alice";
            alice.Age = 18;
            var bob = context.Persons.Create();
            bob.Name = "Bob";
            bob.Age = 20;
            var carol = context.Persons.Create();
            carol.Age = 20;
            carol.Name = "Carol";
            var dave = context.Persons.Create();
            dave.Age = 22;
            dave.Name = "Dave";
            var edith = context.Persons.Create();
            edith.Age = 21;
            edith.Name = null;
            alice.Friends.Add(bob);
            alice.Friends.Add(carol);
            bob.Friends.Add(alice);
            bob.Friends.Add(carol);
            carol.Friends.Add(alice);
            carol.Friends.Add(bob);
            carol.Friends.Add(dave);
            dave.Friends.Add(edith);
            context.SaveChanges();

            var count = context.Persons.Count(e => e.Name == null);
            Assert.Equal(1, count);

            var count2 = context.Persons.Count(e => null == e.Name);
            Assert.Equal(1, count2);
        }

        [Fact]
        public void TestLinqRetrieveId()
        {
            var connectionString = GetConnectionString("TestLinqRetrieveId");
            using (var context = new MyEntityContext(connectionString))
            {
                var alice = new Person {Id = "alice", Name = "Alice"};
                context.Persons.Add(alice);
                context.SaveChanges();
            
                var entity = context.Persons.First();
                Assert.Equal("alice", entity.Id);
                var id = context.Persons.Select(x=>x.Id).First();
                Assert.Equal("alice", id);
            }
        }

    }

}
