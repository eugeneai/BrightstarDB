using System;

namespace BrightstarDB.Storage
{
#if !SILVERLIGHT && !PORTABLE && !NETCORE
    [Serializable]
#endif
    internal class InvalidCommitPointException : BrightstarInternalException
    {
        public InvalidCommitPointException(string msg) : base(msg){}
        public InvalidCommitPointException(string msg, Exception inner) : base(msg, inner) {}
    }
}
