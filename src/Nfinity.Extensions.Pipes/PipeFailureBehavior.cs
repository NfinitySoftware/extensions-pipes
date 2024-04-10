namespace Nfinity.Extensions.Pipes
{
    /// <summary>
    /// The behavior of the processing pipeline should an action fail.
    /// </summary>
    public enum PipeFailureBehavior
    {
        /// <summary>
        /// The default behavior, which is equal to <see cref="FailLastOnly"/>.
        /// </summary>
        Default,

        /// <summary>
        /// Only execute the failure action for the last action that failed. This is the default behavior.
        /// </summary>
        FailLastOnly = Default,

        /// <summary>
        /// Execute the failure action for the last action that failed, as well as all previous actions (to the start of the pipeline).
        /// </summary>
        FailAll
    }
}
