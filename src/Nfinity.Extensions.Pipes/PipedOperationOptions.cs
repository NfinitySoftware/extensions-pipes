namespace Nfinity.Extensions.Pipes
{
    public sealed class PipedOperationOptions
    {
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
