using System;
using System.Linq;
using BrightstarDB.Client;
using Xunit;

namespace BrightstarDB.Tests.EntityFramework
{
    [Collection("BrightstarService")]
    public class ExtendedContextTests : IDisposable
    {
        private readonly string _storeName;

        public ExtendedContextTests()
        {
            _storeName = "ExtendedContextTests" + Guid.NewGuid();
            using (var context = GetContext())
            {
                // Put in data
                context.Companies.Add(new Company
                    {
                        Name = "NetworkedPlanet",
                        TickerSymbol = "NP",
                        HeadCount = 4,
                        CurrentSharePrice = 1.0m
                    });
                var ftse = context.Markets.Create();
                ftse.Name = "FTSE";
                var cac = new Company
                    {
                        Name = "CAC Limited",
                        TickerSymbol = "CAC",
                        ListedOn = ftse,
                        HeadCount = 200,
                        CurrentSharePrice = 1.0m
                    };
                context.Companies.Add(cac);
                context.SaveChanges();
            }
        }

        public void Dispose()
        {
            BrightstarService.Shutdown(false);
        }

        private MyEntityContext GetContext()
        {
            return new MyEntityContext("type=embedded;storesDirectory=c:\\brightstar;storeName=" + _storeName);            
        }

        [Fact]
        public void TestSelectProperty()
        {
            using (var context = GetContext())
            {
                var q = from x in context.Companies select x.Name;
                var results = q.ToList();
                Assert.NotNull(results);
                Assert.Equal(2, results.Count);
                Assert.True(results.Contains("NetworkedPlanet"));
            }
        }

        [Fact]
        public void TestCreateAnonymous()
        {
            using (var context = GetContext())
            {
                var q = from x in context.Companies select new {x.Name, x.TickerSymbol};
                var results = q.ToList();
                Assert.Equal(2, results.Count);
                Assert.True(results.Any(x => x.Name.Equals("NetworkedPlanet")));
                Assert.True(results.Any(x => x.TickerSymbol.Equals("NP")));

                var p = from x in context.Companies select new {x.Name, x.TickerSymbol, Market = x.ListedOn.Name};
                var results2 = p.ToList();
                Assert.Equal(2, results2.Count);
                var npResult = results2.First(x => x.TickerSymbol.Equals("NP"));
                var cacResult = results2.First(x => x.TickerSymbol.Equals("CAC"));
                Assert.Equal("FTSE", cacResult.Market);
                Assert.Null(npResult.Market);

                var r = from x in context.Companies select new {x.Name, x.TickerSymbol, Market = x.ListedOn};
                var results3 = r.ToList();
                Assert.Equal(2, results3.Count);
                var npResult2 = results3.First(x => x.TickerSymbol.Equals("NP"));
                var cacResult2 = results3.First(x => x.TickerSymbol.Equals("CAC"));
                Assert.Null(npResult2.Market);
                Assert.NotNull(cacResult2.Market);
                Assert.Equal("FTSE", cacResult2.Market.Name);
            }
        }

        [Fact]
        public void TestAggregates()
        {
            using (var context = GetContext())
            {
                var averageHeadcount = context.Companies.Average(x => x.HeadCount);
                Assert.Equal(102, averageHeadcount);

                var count = context.Companies.Count();
                Assert.Equal(2, count);

                var largeCompanyCount = context.Companies.Count(x => x.HeadCount > 100);
                Assert.Equal(1, largeCompanyCount);

                var largeCompanyHeadcount = context.Companies.Where(x => x.HeadCount > 100).Average(x => x.HeadCount);
                Assert.Equal(200, largeCompanyHeadcount);

                var companyLongCount = context.Companies.LongCount();
                Assert.Equal(2, companyLongCount);

                var smallCompanyLongCount = context.Companies.Where(x => x.HeadCount < 100).LongCount();
                Assert.Equal(1, smallCompanyLongCount);

                var smallestCompanyHeadcount = context.Companies.Min(x => x.HeadCount);
                Assert.Equal(4, smallestCompanyHeadcount);

                var largestCompanyHeadcount = context.Companies.Max(x => x.HeadCount);
                Assert.Equal(200, largestCompanyHeadcount);
            }
        }

        [Fact]
        public void TestOrdering()
        {
            using (var context = GetContext())
            {
                var orderedCompanies = context.Companies.OrderBy(x => x.HeadCount).ToList();
                Assert.Equal("NP", orderedCompanies[0].TickerSymbol);
                Assert.Equal("CAC", orderedCompanies[1].TickerSymbol);

                orderedCompanies = context.Companies.OrderByDescending(x => x.HeadCount).ToList();
                Assert.Equal("NP", orderedCompanies[1].TickerSymbol);
                Assert.Equal("CAC", orderedCompanies[0].TickerSymbol);

                orderedCompanies = context.Companies.OrderBy(x => x.CurrentSharePrice).ThenBy(x => x.HeadCount).ToList();
                Assert.Equal("NP", orderedCompanies[0].TickerSymbol);
                Assert.Equal("CAC", orderedCompanies[1].TickerSymbol);

                orderedCompanies =
                    context.Companies.OrderBy(x => x.CurrentSharePrice).ThenByDescending(x => x.HeadCount).ToList();
                Assert.Equal("NP", orderedCompanies[1].TickerSymbol);
                Assert.Equal("CAC", orderedCompanies[0].TickerSymbol);
            }

        }

        [Fact]
        public void TestSingle()
        {
            using (var context = GetContext())
            {
                var singleMarket = context.Markets.Single();
                ICompany singleCompany;
                Assert.Equal("FTSE", singleMarket.Name);

                Assert.Throws<InvalidOperationException>(() => context.Companies.Single());
                Assert.Throws<InvalidOperationException>(() => context.Animals.Single());
                
                singleMarket = context.Markets.SingleOrDefault();
                Assert.NotNull(singleMarket);
                Assert.Equal("FTSE", singleMarket.Name);
                Assert.Throws<InvalidOperationException>(() => context.Companies.SingleOrDefault());

                var animal = context.Animals.SingleOrDefault();
                Assert.Null(animal);

                singleCompany = context.Companies.Single(x => x.HeadCount < 100);
                Assert.NotNull(singleCompany);
                Assert.Equal("NP", singleCompany.TickerSymbol);

                singleCompany = context.Companies.SingleOrDefault(x => x.HeadCount == 1);
                Assert.Null(singleCompany);
            }
        }

        [Fact]
        public void TestFirst()
        {
            using (var context = GetContext())
            {
                var firstCo = context.Companies.First();
                Assert.NotNull(firstCo);

                Assert.Throws<InvalidOperationException>(() => context.Animals.First());

                firstCo = context.Companies.First(x => x.HeadCount < 100);
                Assert.NotNull(firstCo);
                Assert.Equal("NP", firstCo.TickerSymbol);

                Assert.Throws<InvalidOperationException>(() => context.Animals.First(x => x.Name.Equals("bob")));
            }
        }
    }
}
