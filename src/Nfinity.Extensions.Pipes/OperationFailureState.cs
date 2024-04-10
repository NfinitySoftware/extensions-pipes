using System.Net;

namespace Nfinity.Extensions.Pipes
{
    public sealed class OperationFailureState
    {
        public string FailureReason { get; init; }
        public Exception Exception { get; init; }
        public HttpStatusCode HttpStatusCode { get; init; }
        public bool IsRetryable { get; init; }

        internal static OperationFailureState FromResult(OperationResult result)
            => result == null ? null : new() 
            { 
                FailureReason = result.FailureReason, 
                Exception = result.Exception, 
                HttpStatusCode = result.HttpStatusCode, 
                IsRetryable = result.IsRetryable 
            };
    }
}
