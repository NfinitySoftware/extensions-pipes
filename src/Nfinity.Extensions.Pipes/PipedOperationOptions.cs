namespace Nfinity.Extensions.Pipes
{
    /// <summary>
    /// Represents options that govern how the pipeline behaves at execution time.
    /// </summary>
    public sealed class PipedOperationOptions
    {
        /// <summary>
        /// Gets the behavior that defines the way in which failure actions are executed in the pipeline.
        /// </summary>
        public PipeFailureBehavior FailureBehaviour { get; } = default;

        internal bool ShouldOnlyFailLast => FailureBehaviour != PipeFailureBehavior.FailAll;

        internal PipedOperationOptions()
        {
        }

        internal PipedOperationOptions(PipeFailureBehavior failureBehaviour)
        {
            FailureBehaviour = failureBehaviour;
        }
    }
}
