using System;
using System.Linq;
using BrightstarDB.EntityFramework;
using Xunit;

namespace BrightstarDB.Tests.EntityFramework
{
    
    public class InterfaceInheritanceTests
    {
        private const string ConnectionString = "type=embedded;storesDirectory=c:\\brightstar\\;storeName=";

        private static string MakeStoreName(string suffix)
        {
            var dt = DateTime.Now;
            return String.Format("{0:02d}{1:02d}{2:02d}_{3}", dt.Hour, dt.Minute, dt.Second, suffix);
        }

        [Fact]
        public void TestRetrieveDerivedInstancesFromBaseCollection()
        {
            var storeName = MakeStoreName("retrieveDerviedInstances");
            var context = new MyEntityContext(ConnectionString + storeName);
            var derivedEntity = context.DerivedEntities.Create();
            derivedEntity.BaseStringValue = "This is a dervied entity";
            derivedEntity.DateTimeProperty = new DateTime(2011, 11, 11);
            var baseEntity = context.BaseEntities.Create();
            baseEntity.BaseStringValue = "This is a base entity";
            context.SaveChanges();

            context = new MyEntityContext(ConnectionString+storeName);

            var baseEntities = context.BaseEntities.ToList();
            Assert.Equal(2,baseEntities.Count);
            Assert.True(baseEntities.Any(x=>x.BaseStringValue.Equals("This is a base entity")));
            Assert.True(baseEntities.Any(x=>x.BaseStringValue.Equals("This is a dervied entity")));

            var derivedEntities = context.DerivedEntities.ToList();
            Assert.Equal(1, derivedEntities.Count);
            Assert.True(derivedEntities.Any(x=>x.BaseStringValue.Equals("This is a dervied entity")));
        }

        [Fact]
        public void TestUseDerivedInstanceInBaseClassCollectionProperty()
        {
            var storeName = MakeStoreName("useDerivedInstance");
            var context = new MyEntityContext(ConnectionString + storeName);
            var entity1 = context.DerivedEntities.Create();
            entity1.BaseStringValue = "Entity1";
            var entity2 = context.DerivedEntities.Create();
            entity2.BaseStringValue = "Entity2";
            var entity3 = context.BaseEntities.Create();
            entity3.BaseStringValue = "Entity3";
            entity1.RelatedEntities.Add(entity2);
            entity1.RelatedEntities.Add(entity3);
            context.SaveChanges();

            context=new MyEntityContext(ConnectionString + storeName);
            var baseEntities = context.BaseEntities.ToList();
            Assert.Equal(3, baseEntities.Count);
            var derivedEntities = context.DerivedEntities.ToList();
            Assert.Equal(2, derivedEntities.Count);
            entity1 = context.DerivedEntities.Where(x => x.BaseStringValue.Equals("Entity1")).FirstOrDefault();
            Assert.NotNull(entity1);
            Assert.Equal(2, entity1.RelatedEntities.Count);
            Assert.True(entity1.RelatedEntities.Any(x=>x.BaseStringValue.Equals("Entity2")));
            Assert.True(entity1.RelatedEntities.Any(x=>x.BaseStringValue.Equals("Entity3")));

        }

        [Fact]
        public void TestIdentifierPrefixOnBaseEntity()
        {
            var storeName = MakeStoreName("IdentifierPrefixOnBaseEntity");
            using (var context = new MyEntityContext(ConnectionString + storeName))
            {
                var entity1 = new DerivedEntity {Id = "entity1"};
                context.DerivedEntities.Add(entity1);
                entity1.BaseStringValue = "Entity1";
                context.SaveChanges();
            }

            var doContext = BrightstarDB.Client.BrightstarService.GetDataObjectContext(ConnectionString + storeName);
            var store = doContext.OpenStore(storeName);
            var dataObject = store.GetDataObject("http://example.org/entities/entity1");
            Assert.NotNull(dataObject);
        }

        [Fact]
        public void TestBecomeAndUnbecome()
        {
            var storeName = MakeStoreName("becomeAndUnbecome");
            var context = new MyEntityContext(ConnectionString + storeName);
            var entity1 = context.BaseEntities.Create();
            entity1.BaseStringValue = "BecomeTest";
            context.SaveChanges();

            context = new MyEntityContext(ConnectionString + storeName);
            Assert.Equal(1, context.BaseEntities.Count());
            Assert.Equal(0, context.DerivedEntities.Count());
            var entity =
                context.BaseEntities.Where(x => x.BaseStringValue.Equals("BecomeTest")).FirstOrDefault();
            var derived = (entity as BrightstarEntityObject).Become<IDerivedEntity>();
            derived.DateTimeProperty = new DateTime(2011, 11,11);
            context.SaveChanges();

            context = new MyEntityContext(ConnectionString + storeName);
            Assert.Equal(1, context.BaseEntities.Count());
            Assert.Equal(1, context.DerivedEntities.Count());
            entity =
                context.BaseEntities.Where(x => x.BaseStringValue.Equals("BecomeTest")).FirstOrDefault();
            Assert.Equal("BecomeTest", entity.BaseStringValue);
            var derivedEntity = (entity as BrightstarEntityObject).Become<IDerivedEntity>();
            Assert.Equal("BecomeTest", derivedEntity.BaseStringValue);
            Assert.Equal(new DateTime(2011, 11, 11), derivedEntity.DateTimeProperty);

            context.SaveChanges();

            context = new MyEntityContext(ConnectionString + storeName);
            var d2 = context.DerivedEntities.Where(x => x.BaseStringValue.Equals("BecomeTest")).FirstOrDefault();
            Assert.NotNull(d2);
            (d2 as BrightstarEntityObject).Unbecome<IDerivedEntity>();
            context.SaveChanges();

            context = new MyEntityContext(ConnectionString + storeName);
            Assert.Equal(1, context.BaseEntities.Count());
            Assert.Equal(0, context.DerivedEntities.Count());

        }
    }
}
