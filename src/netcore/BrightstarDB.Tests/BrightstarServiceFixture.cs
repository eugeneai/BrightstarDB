using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrightstarDB.Client;
using Xunit;

namespace BrightstarDB.Tests
{
    public class BrightstarServiceFixture : IDisposable
    {
        public void Dispose()
        {
            BrightstarService.Shutdown();
        }
    }

    [CollectionDefinition("BrightstarService")]
    public class BrightstarServiceCollection : ICollectionFixture<BrightstarServiceFixture> { }
}
