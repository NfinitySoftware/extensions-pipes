using System.Net;

namespace Nfinity.Extensions.Pipes
{
    /// <summary>
    /// Represents the result of a method executed in an asynchronous pipeline.
    /// </summary>
    public class OperationResult
    {
        private readonly static OperationResult _success = new() { IsSuccess = true, HttpStatusCode = HttpStatusCode.OK };

        /// <summary>
        /// Gets a value indicating if the operation was successful.
        /// </summary>
        public required bool IsSuccess { get; init; }

        /// <summary>
        /// Gets the descriptive reason for which the operation failed.
        /// </summary>
        public string FailureReason { get; init; }

        /// <summary>
        /// Gets a reference to the <see cref="System.Exception"/> associated with the failed operation.
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// Gets the <see cref="System.Net.HttpStatusCode"/> associated with the failed operation.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; init; }

        /// <summary>
        /// Gets a value indicating whether the failed operation can be retried.
        /// </summary>
        public bool IsRetryable { get; init; }

        /// <summary>
        /// Gets a reference to the data associated with the operation, which can be passed forward to the next method in the pipeline.
        /// </summary>
        public object Data { get; init; }

        /// <summary>
        /// Returns a reference to the singleton <see cref="OperationResult"/> instance that represents a 
        /// successful operation, with an <see cref="HttpStatusCode"/> equal to <see cref="System.Net.HttpStatusCode.OK"/>.
        /// </summary>
        public static OperationResult Success()
            => _success;

        /// <summary>
        /// Creates and returns an instance of <see cref="OperationResult"/> with the given data.
        /// </summary>
        /// <param name="data">
        /// The data that represents some useful state that can be passed forward to the next method in the pipeline.
        /// </param>
        public static OperationResult Success(object data)
            => data == null ? _success : new() { IsSuccess = true, Data = data, HttpStatusCode = HttpStatusCode.OK };

        /// <summary>
        /// Creates and returns an instance of <see cref="OperationResult"/> that represents a failed operation,
        /// with the given parameters.
        /// </summary>
        /// <param name="exception">The exception that was caught or created, if any.</param>
        /// <param name="reason">A descriptive reason for which the operation failed.</param>
        /// <param name="data">
        /// The data that represents some useful state that can be passed forward to the next method in the pipeline.
        /// </param>
        /// <param name="isRetryable">Whether the operation can be retried once the pipeline has completed.</param>
        /// <param name="statusCode">The HTTP status code associated with the failed state of the operation.</param>
        public static OperationResult Fail(Exception exception = null, string reason = null, object data = null, 
            bool isRetryable = true, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            => new() 
            { 
                IsSuccess = false,
                FailureReason = reason, 
                Exception = exception, 
                Data = data, 
                IsRetryable = isRetryable, 
                HttpStatusCode = statusCode 
            };
    }
}
