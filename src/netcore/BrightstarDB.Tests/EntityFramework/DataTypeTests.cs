using System;
using System.Linq;
using BrightstarDB.Client;
using Xunit;

namespace BrightstarDB.Tests.EntityFramework
{
    [Collection("BrightstarService")]
    public class DataTypeTests : IDisposable
    {
        private MyEntityContext _myEntityContext;

        private readonly string _connectionString;

        public DataTypeTests()
        {
            _connectionString = $"Type=embedded;StoresDirectory={Configuration.StoreLocation};StoreName=DataTypeTests{DateTime.Now.Ticks}";
            SetUp();
        }

        private void SetUp()
        {
            _myEntityContext = new MyEntityContext(_connectionString);
        }

        public void Dispose()
        {
            BrightstarService.Shutdown(false);
        }

        [Fact]
        public void TestCreateAndSetProperties()
        {
            var entity = _myEntityContext.TestEntities.Create();
            var now = DateTime.Now;
            entity.SomeDateTime = now;
            entity.SomeDecimal = 3.14m;
            entity.SomeDouble = 3.14;
            entity.SomeFloat = 3.14F;
            entity.SomeInt = 3;
            entity.SomeNullableDateTime = null;
            entity.SomeNullableInt = null;
            entity.SomeString = "test entity";

            entity.SomeBool = true;
            entity.SomeLong = 50L;

            _myEntityContext.SaveChanges();
            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            var checkEntity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));

            Assert.NotNull(checkEntity);
            Assert.NotNull(checkEntity.SomeDateTime);
            Assert.NotNull(checkEntity.SomeDecimal);
            Assert.NotNull(checkEntity.SomeDouble);
            Assert.NotNull(checkEntity.SomeFloat);
            Assert.NotNull(checkEntity.SomeInt);
            Assert.Null(checkEntity.SomeNullableDateTime);
            Assert.Null(checkEntity.SomeNullableInt);
            Assert.NotNull(checkEntity.SomeString);

            Assert.Equal(now, checkEntity.SomeDateTime);
            Assert.Equal(3.14m, checkEntity.SomeDecimal);
            Assert.Equal(3.14, checkEntity.SomeDouble);
            Assert.Equal(3.14F, checkEntity.SomeFloat);
            Assert.Equal(3, checkEntity.SomeInt);
            Assert.Equal("test entity", checkEntity.SomeString);
            Assert.True(checkEntity.SomeBool);
            Assert.Equal(50L, checkEntity.SomeLong);          
        }

        [Fact]
        public void TestIssue128CannotSetFloatValueBelow1()
        {
            // Create an entity
            var entity = _myEntityContext.TestEntities.Create();
            // Set the properties that allow fractional values to values < 1.0
            entity.SomeDecimal = 0.14m;
            entity.SomeDouble = 0.14;
            entity.SomeFloat = 0.14F;
            // Persist the changes
            _myEntityContext.SaveChanges();
            var entityId = entity.Id;

            // Create a new context connection so that we don't get a locally cached value from the context
            var newContext = new MyEntityContext(_connectionString);
            // Retrieve the previously created entity
            var checkEntity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));

            // Assert that the entity was found and the values we set are set to the values we originally provided
            Assert.NotNull(checkEntity);
            Assert.NotNull(checkEntity.SomeDecimal);
            Assert.NotNull(checkEntity.SomeDouble);
            Assert.NotNull(checkEntity.SomeFloat);
            Assert.Equal(0.14m, checkEntity.SomeDecimal);
            Assert.Equal(0.14, checkEntity.SomeDouble);
            Assert.Equal(0.14F, checkEntity.SomeFloat);
        }

        [Fact]
        public void TestCreateAndSetCollections()
        {
            var entity = _myEntityContext.TestEntities.Create();

            var now = DateTime.Now;
            for(var i = 0; i<10;i++)
            {
                var date = now.AddDays(i);    
                entity.CollectionOfDateTimes.Add(date);
            }
            for (var i = 0; i < 10; i++)
            {
                var dec = i + .5m;
                entity.CollectionOfDecimals.Add(dec);
            }
            for (var i = 0; i < 10; i++)
            {
                var dbl = i + .5;
                entity.CollectionOfDoubles.Add(dbl);
            }
            for (var i = 0; i < 10; i++)
            {
                var flt = i + .5F;
                entity.CollectionOfFloats.Add(flt);
            }
            for (var i = 0; i < 10; i++)
            {
                entity.CollectionOfInts.Add(i);
            }
            entity.CollectionOfBools.Add(true);
            entity.CollectionOfBools.Add(false);
            for (var i = 0; i < 10; i++)
            {
                var l = i*100;
                entity.CollectionOfLong.Add(l);
            }
            for (var i = 0; i < 10; i++)
            {
                var s = "word" + i;
                entity.CollectionOfStrings.Add(s);
            }

            _myEntityContext.SaveChanges();
            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            var checkEntity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));

            Assert.NotNull(checkEntity);
            Assert.NotNull(checkEntity.CollectionOfDateTimes);
            Assert.NotNull(checkEntity.CollectionOfDecimals);
            Assert.NotNull(checkEntity.CollectionOfDoubles);
            Assert.NotNull(checkEntity.CollectionOfFloats);
            Assert.NotNull(checkEntity.CollectionOfInts);
            Assert.NotNull(checkEntity.CollectionOfBools);
            Assert.NotNull(checkEntity.CollectionOfLong);
            Assert.NotNull(checkEntity.CollectionOfStrings);

            var lstDateTimes = checkEntity.CollectionOfDateTimes.OrderBy(e => e).ToList();
            var lstDecs = checkEntity.CollectionOfDecimals.OrderBy(e => e).ToList();
            var lstDbls = checkEntity.CollectionOfDoubles.OrderBy(e => e).ToList();
            var lstFloats = checkEntity.CollectionOfFloats.OrderBy(e => e).ToList();
            var lstInts = checkEntity.CollectionOfInts.OrderBy(e => e).ToList();
            var lstLongs = checkEntity.CollectionOfLong.OrderBy(e => e).ToList();
            var lstStrings = checkEntity.CollectionOfStrings.OrderBy(e => e).ToList();
            var lstBools = checkEntity.CollectionOfBools.OrderBy(e => e).ToList();
            for (var i = 0; i < 10; i++)
            {
                var date = now.AddDays(i);
                var dec = i + .5m;
                var dbl = i + .5;
                var flt = i + .5F;
                var l = i * 100;
                var s = "word" + i;

                Assert.Equal(date, lstDateTimes[i]);
                Assert.Equal(dec, lstDecs[i]);
                Assert.Equal(dbl, lstDbls[i]);
                Assert.Equal(flt, lstFloats[i]);
                Assert.Equal(l, lstLongs[i]);
                Assert.Equal(i, lstInts[i]);
                Assert.Equal(s, lstStrings[i]);
            }
            Assert.Equal(2, lstBools.Count);
            
        }

        [Fact]
        public void TestSetByte()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeByte = 255;
            entity.AnotherByte = 128;
            entity.NullableByte = null;
            entity.AnotherNullableByte = null;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.NotNull(entity);
            Assert.NotNull(entity.SomeByte);
            Assert.NotNull(entity.AnotherByte);

            Assert.Equal(255, entity.SomeByte);
            Assert.Equal(128, entity.AnotherByte);

            Assert.Null(entity.NullableByte);
            Assert.Null(entity.AnotherNullableByte);
        }

        [Fact]
        public void TestSetChar()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeChar = 'C';
            entity.AnotherChar = 'c';
            entity.NullableChar = null;
            entity.AnotherNullableChar = null;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.NotNull(entity);
            Assert.NotNull(entity.SomeChar);
            Assert.NotNull(entity.AnotherChar);
            Assert.Null(entity.NullableChar);
            Assert.Null(entity.AnotherNullableChar);
            
            Assert.Equal('C', entity.SomeChar);
            Assert.Equal('c', entity.AnotherChar);

            Assert.NotEqual('c', entity.SomeChar);
            Assert.NotEqual('C', entity.AnotherChar);

        }

        [Fact]
        public void TestSetSbyte()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeSByte = 127;
            entity.AnotherSByte = 64;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.NotNull(entity);
            Assert.NotNull(entity.SomeSByte);
            Assert.NotNull(entity.AnotherSByte);
            
            Assert.Equal(127, entity.SomeSByte);
            Assert.Equal(64, entity.AnotherSByte);

            Assert.NotEqual(64, entity.SomeSByte);
            Assert.NotEqual(127, entity.AnotherSByte);

        }

        [Fact]
        public void TestSetShort()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeShort = 32767;
            entity.AnotherShort = -32768;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.NotNull(entity);
            Assert.NotNull(entity.SomeShort);
            Assert.NotNull(entity.AnotherShort);

            Assert.Equal(32767, entity.SomeShort);
            Assert.Equal(-32768, entity.AnotherShort);

            Assert.NotEqual(-32768, entity.SomeShort);
            Assert.NotEqual(32767, entity.AnotherShort);
        }

        [Fact]
        public void TestSetUint()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeUInt = 4294967295;
            entity.AnotherUInt = 12;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.NotNull(entity);
            Assert.NotNull(entity.SomeUInt);
            Assert.NotNull(entity.AnotherUInt);

            Assert.Equal(4294967295U, entity.SomeUInt);
            Assert.Equal(12U, entity.AnotherUInt);

            Assert.NotEqual(12U, entity.SomeUInt);
            Assert.NotEqual(4294967295U, entity.AnotherUInt);
        }

        [Fact]
        public void TestSetUlong()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeULong = 18446744073709551615;
            entity.AnotherULong = 52;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.NotNull(entity);
            Assert.NotNull(entity.SomeULong);
            Assert.NotNull(entity.AnotherULong);

            Assert.Equal(18446744073709551615, entity.SomeULong);
            Assert.Equal(52UL, entity.AnotherULong);

            Assert.NotEqual(52UL, entity.SomeULong);
            Assert.NotEqual(18446744073709551615, entity.AnotherULong);
        }

        [Fact]
        public void TestSetUShort()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeUShort = 65535;
            entity.AnotherUShort = 52;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.NotNull(entity);
            Assert.NotNull(entity.SomeUShort);
            Assert.NotNull(entity.AnotherUShort);

            Assert.Equal(65535, entity.SomeUShort);
            Assert.Equal(52, entity.AnotherUShort);

            Assert.NotEqual(52, entity.SomeUShort);
            Assert.NotEqual(65535, entity.AnotherUShort);
        }

        [Fact]
        public void TestEnums()
        {
            var entity1 = _myEntityContext.TestEntities.Create();
            entity1.SomeEnumeration = TestEnumeration.Second;
            entity1.SomeNullableEnumeration = TestEnumeration.Third;
            entity1.SomeFlagsEnumeration = TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagB;
            entity1.SomeNullableFlagsEnumeration = TestFlagsEnumeration.FlagB | TestFlagsEnumeration.FlagC;
            entity1.SomeSystemEnumeration = DayOfWeek.Friday;
            entity1.SomeNullableSystemEnumeration = DayOfWeek.Friday;
            var entity2 = _myEntityContext.TestEntities.Create();
            _myEntityContext.SaveChanges();

            var entity1Id = entity1.Id;
            var entity2Id = entity2.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity1 = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entity1Id));
            entity2 = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entity2Id));
            Assert.NotNull(entity1);
            Assert.NotNull(entity2);
            Assert.Equal(TestEnumeration.Second, entity1.SomeEnumeration);
            Assert.Equal(TestEnumeration.Third, entity1.SomeNullableEnumeration);
            Assert.Equal(TestFlagsEnumeration.FlagB|TestFlagsEnumeration.FlagA, entity1.SomeFlagsEnumeration);
            Assert.Equal(TestFlagsEnumeration.FlagC|TestFlagsEnumeration.FlagB, entity1.SomeNullableFlagsEnumeration);
            Assert.Equal(DayOfWeek.Friday, entity1.SomeSystemEnumeration);
            Assert.Equal(DayOfWeek.Friday, entity1.SomeNullableSystemEnumeration);

            Assert.Equal(TestEnumeration.First, entity2.SomeEnumeration);
            Assert.Null(entity2.SomeNullableEnumeration);
            Assert.Equal(TestFlagsEnumeration.NoFlags, entity2.SomeFlagsEnumeration);
            Assert.Null(entity2.SomeNullableFlagsEnumeration);
            Assert.Equal(DayOfWeek.Sunday, entity2.SomeSystemEnumeration);
            Assert.Null(entity2.SomeNullableSystemEnumeration);
        }
        //note Test for SetByteArray and SetEnumeration are in SimpleContextTests.cs

    }
}
