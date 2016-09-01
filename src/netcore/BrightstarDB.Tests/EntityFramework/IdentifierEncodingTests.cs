using System;
using System.Linq;
using BrightstarDB.EntityFramework;
using Xunit;

namespace BrightstarDB.Tests.EntityFramework
{
    
    public class IdentifierEncodingTests : IDisposable
    {
        private MyEntityContext _myEntityContext;

        private readonly string _connectionString =
            "Type=embedded;StoresDirectory=c:\\brightstar;StoreName=IdentifierEncodingTests_" + DateTime.Now.Ticks;

        public IdentifierEncodingTests()
        {
            _myEntityContext = new MyEntityContext(_connectionString);
        }

        public void TearDown()
        {
            _myEntityContext.Dispose();
        }

        [Fact]
        public void TestCreateItemWithSpecialCharactersInIdentifier()
        {
            var person = new DBPediaPerson
                             {
                                 Id = "Aleksandar_Đorđević",
                                 Name = "Aleksandar Djordjevic",
                                 GivenName = "Aleksandar",
                                 Surname = "Djordjevic"
                             };
            _myEntityContext.DBPediaPersons.Add(person);
            _myEntityContext.SaveChanges();

            // Try to retrieve by Id 
            var found = _myEntityContext.DBPediaPersons.FirstOrDefault(p => p.Id.Equals("Aleksandar_Đorđević"));
            Assert.NotNull(found);
            Assert.Equal("Aleksandar", found.GivenName);
            Assert.Equal("Aleksandar_Đorđević", found.Id);

        }


        public void Dispose()
        {
            TearDown();
        }
    }
}
