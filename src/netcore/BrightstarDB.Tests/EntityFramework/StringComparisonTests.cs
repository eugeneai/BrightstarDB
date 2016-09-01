using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace BrightstarDB.Tests.EntityFramework
{
    
    public class StringComparisonTests
    {
        private MyEntityContext _context;

        public StringComparisonTests()
        {
            SetUp();
        }

        public void SetUp()
        {
            _context = new MyEntityContext("type=embedded;storesDirectory=" + Configuration.StoreLocation + ";storeName=EFStringComparisonTests_" + DateTime.Now.Ticks);
            var np = new Company {Name = "NetworkedPlanet"};
            var apple = new Company {Name = "Apple"};
            _context.Companies.Add(np);
            _context.Companies.Add(apple);
            _context.SaveChanges();
        }

        [Fact]
        public void TestStartsWith()
        {
            var results = _context.Companies.Where(c => c.Name.StartsWith("Net")).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet",results[0].Name);

#if !NETCORE
            results = _context.Companies.Where(c => c.Name.StartsWith("net", true, CultureInfo.CurrentCulture)).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", false, CultureInfo.CurrentCulture)).ToList();
            Assert.Equal(0, results.Count);
#endif

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.CurrentCultureIgnoreCase)).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.CurrentCulture)).ToList();
            Assert.Equal(0, results.Count);

#if !NETCORE
#if !PORTABLE // InvariantCultureIgnoreCase is not supported by PCL
            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.InvariantCultureIgnoreCase)).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet", results[0].Name);
#endif

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.InvariantCulture)).ToList();
            Assert.Equal(0, results.Count);
#endif
            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.Ordinal)).ToList();
            Assert.Equal(0, results.Count);
        }

        [Fact]
        public void TestEndsWith()
        {
            var results = _context.Companies.Where(c => c.Name.EndsWith("net")).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net")).ToList();
            Assert.Equal(0, results.Count);

#if !NETCORE
            results = _context.Companies.Where(c => c.Name.EndsWith("Net", true, CultureInfo.CurrentCulture)).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", false, CultureInfo.CurrentCulture)).ToList();
            Assert.Equal(0, results.Count);
#endif
            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.CurrentCultureIgnoreCase)).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.CurrentCulture)).ToList();
            Assert.Equal(0, results.Count);

#if !NETCORE
#if !PORTABLE // InvariantCultureIgnoreCase is not supported by PCL
            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.InvariantCultureIgnoreCase)).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet", results[0].Name);
#endif

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.InvariantCulture)).ToList();
            Assert.Equal(0, results.Count);
#endif
            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.Ordinal)).ToList();
            Assert.Equal(0, results.Count);
        }

        [Fact]
        public void TestContains()
        {
            var results = _context.Companies.Where(c => c.Name.Contains("Pl")).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.Contains("pl")).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("Apple", results[0].Name);

        }

        [Fact]
        public void TestStringLengthFilter()
        {
            var results = _context.Companies.Where(c => c.Name.Length > 10).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.Length<10).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal("Apple", results[0].Name);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
