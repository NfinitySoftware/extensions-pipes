namespace Nfinity.Extensions.Pipes
{
    /// <summary>
    /// Represents the result of a method executed in an asynchronous pipeline.
    /// </summary>
    public class TaskResult
    {
        private readonly static TaskResult _success = new() { IsSuccess = true };

        /// <summary>
        /// Gets a value indicating if the operation was successful.
        /// </summary>
        public required bool IsSuccess { get; init; }

        /// <summary>
        /// Gets a reference to the <see cref="System.Exception"/> associated with the failed operation.
        /// </summary>
        public Exception Exception { get; init; }
        
        /// <summary>
        /// Returns a reference to the singleton <see cref="TaskResult"/> instance that represents a 
        /// successful operation.
        /// </summary>
        public static TaskResult Success()
            => _success;

        /// <summary>
        /// Creates and returns an instance of <see cref="TaskResult"/> that represents a failed operation,
        /// with the given parameters.
        /// </summary>
        /// <param name="exception">The exception that was caught or created, if any.</param>
        public static TaskResult Fail(Exception exception = null)
            => new() 
            { 
                IsSuccess = false,
                Exception = exception
            };
    }
}
