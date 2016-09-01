using System.Threading;
using BrightstarDB.Client;
using Xunit;

namespace BrightstarDB.Tests
{
    internal static class TestHelper
    {
        public static void AssertJobCompletesSuccessfully(IBrightstarService client, string storeName, IJobInfo job)
        {
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Thread.Sleep(10);
                job = client.GetJobInfo(storeName, job.JobId);
            }
            Assert.True(job.JobCompletedOk, string.Format("Expected job to complete successfully, but it failed with message '{0}' : {1}", job.StatusMessage, job.ExceptionInfo));
        }
    }
}