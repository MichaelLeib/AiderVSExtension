using System;

namespace AiderVSExtension.Exceptions
{
    /// <summary>
    /// Base exception for Aider service related errors
    /// </summary>
    public class AiderServiceException : Exception
    {
        public AiderServiceException() : base() { }

        public AiderServiceException(string message) : base(message) { }

        public AiderServiceException(string message, Exception innerException) : base(message, innerException) { }
    }


}