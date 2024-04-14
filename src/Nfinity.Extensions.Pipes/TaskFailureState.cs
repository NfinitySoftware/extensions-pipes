namespace Nfinity.Extensions.Pipes
{
    /// <summary>
    /// Represents the overall failure state of a pipeline.
    /// </summary>
    public sealed class TaskFailureState
    {
        /// <summary>
        /// Gets a reference to an <see cref="AggregateException"/> instance containing the exceptions associated 
        /// with all operations that failed.
        /// </summary>
        public AggregateException Exception { get; init; }

        internal static TaskFailureState FromResult(TaskResult result)
            => result == null ? null : new() { Exception = result.Exception as AggregateException };
    }
}
