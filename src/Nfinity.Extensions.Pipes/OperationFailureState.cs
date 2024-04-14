using System.Net;

namespace Nfinity.Extensions.Pipes
{
    /// <summary>
    /// Represents the overall failure state of a pipeline.
    /// </summary>
    public sealed class OperationFailureState
    {
        /// <summary>
        /// Gets the descriptive reason for which the pipeline failed. This will be equal to either the
        /// <see cref="OperationResult.FailureReason"/> or <see cref="System.Exception.Message"/> of the first operation that failed
        /// (whichever is available, in this order of precedence).
        /// </summary>
        public string FailureReason { get; init; }

        /// <summary>
        /// Gets a reference to an <see cref="AggregateException"/> instance containing the exceptions associated 
        /// with all operations that failed.
        /// </summary>
        public AggregateException Exception { get; init; }

        /// <summary>
        /// Gets the <see cref="System.Net.HttpStatusCode"/> of the first operation that failed.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; init; }

        /// <summary>
        /// Gets a value indicating if any operation was reported to be retryable.
        /// </summary>
        public bool IsAnyRetryable { get; init; }

        internal static OperationFailureState FromResult(OperationResult result)
            => result == null ? null : new() 
            { 
                FailureReason = result.FailureReason, 
                Exception = result.Exception as AggregateException,
                HttpStatusCode = result.HttpStatusCode,
                IsAnyRetryable = result.IsRetryable 
            };
    }
}
