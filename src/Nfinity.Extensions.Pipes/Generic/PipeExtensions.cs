using System.Reflection;

namespace Nfinity.Extensions.Pipes.Generic
{
    public static class PipeExtensions
    {
        /// <summary>
        /// An alternative method for starting a pipeline.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedResult<TOut>> PipeAsync<TIn, TOut>(this Func<Task<TIn>> first, Func<TIn, Task<TOut>> next)
        {
            var pipedResult = await ExecuteAsync(first);
            if (!pipedResult.IsSuccess) return PipedResult<TOut>.Fail();

            var nextResult = await ExecuteAsync(next, pipedResult.Data);
            return nextResult;
        }

        /// <summary>
        /// An alternative method for starting a pipeline.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedResult> PipeAsync<T>(this Func<Task<T>> first, Func<Task> next)
        {
            var pipedResult = await ExecuteAsync(first);
            if (!pipedResult.IsSuccess) return pipedResult;

            return await ExecuteAsync(next);
        }

        /// <summary>
        /// An alternative method for starting a pipeline.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedResult> PipeAsync(this Func<Task> first, Func<Task> next)
        {
            var pipedResult = await ExecuteAsync(first);
            if (!pipedResult.IsSuccess) return pipedResult;
            return await ExecuteAsync(next);
        }

        /// <summary>
        /// An alternative method for starting a pipeline.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedResult<T>> PipeAsync<T>(this Func<Task> first, Func<Task<T>> next)
        {
            var pipedResult = await ExecuteAsync(first);
            if (!pipedResult.IsSuccess) return PipedResult<T>.Fail();

            return await ExecuteAsync(next);
        }

        /// <summary>
        /// Enqueues a method in the pipeline.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedResult<TOut>> PipeAsync<TIn, TOut>(this Task<PipedResult<TIn>> result, Func<TIn, Task<TOut>> next)
        {
            var pipedResult = await result;
            if (!pipedResult.IsSuccess) return PipedResult<TOut>.Fail();

            var nextResult = await ExecuteAsync(next, pipedResult.Data);
            return nextResult;
        }

        /// <summary>
        /// Enqueues a method in the pipeline.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedResult> PipeAsync<T>(this Task<PipedResult<T>> result, Func<T, Task> next)
        {
            var pipedResult = await result;
            if (!pipedResult.IsSuccess) return pipedResult;

            var nextResult = await ExecuteAsync(next, pipedResult.Data);
            return nextResult;
        }

        /// <summary>
        /// Enqueues a method in the pipeline.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedResult> PipeAsync<T>(this Task<PipedResult<T>> result, Func<Task> next)
        {
            var pipedResult = await result;
            if (!pipedResult.IsSuccess) return pipedResult;

            return await ExecuteAsync(next);
        }

        /// <summary>
        /// Enqueues a method in the pipeline.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedResult> PipeAsync(this Task<PipedResult> result, Func<Task> next)
        {
            var pipedResult = await result;
            if (!pipedResult.IsSuccess) return pipedResult;

            return await ExecuteAsync(next);
        }

        /// <summary>
        /// Enqueues a method in the pipeline.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedResult<T>> PipeAsync<T>(this Task<PipedResult> result, Func<Task<T>> next)
        {
            var pipedResult = await result;
            if (!pipedResult.IsSuccess) return PipedResult<T>.Fail();

            return await ExecuteAsync(next);
        }

        /// <summary>
        /// Enqueues a failure handler method to execute should the preceding method fail.
        /// </summary>
        /// <typeparam name="T">The type of the data contained in the result.</typeparam>
        /// <param name="result"></param>
        /// <param name="onFail">The method to execute.</param>
        public static async Task<PipedResult<T>> OnFailAsync<T>(this Task<PipedResult<T>> result, Func<PipedResult<T>, Task<PipedResult<T>>> onFail)
        {
            var pipedResult = await result;

            pipedResult.EnqueueFailAction(onFail.GetMethodInfo(), pipedResult.Data);
            if (!pipedResult.IsSuccess) return pipedResult;

            return await ExecutePassThroughAsync(onFail, pipedResult);
        }

        /// <summary>
        /// Enqueues a failure handler method to execute should the preceding method fail.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="onFail">The method to execute.</param>
        public static async Task<PipedResult> OnFailAsync(this Task<PipedResult> result, Func<PipedResult, Task<PipedResult>> onFail)
        {
            var pipedResult = await result;

            pipedResult.EnqueueFailAction(onFail.GetMethodInfo());
            if (!pipedResult.IsSuccess) return pipedResult;

            return await ExecutePassThroughAsync(onFail, pipedResult);
        }

        /// <summary>
        /// Enqueues a failure handler method to execute should the preceding method fail.
        /// </summary>
        /// <typeparam name="T">The type of the data contained in the result.</typeparam>
        /// <param name="result"></param>
        /// <param name="final">The method to execute.</param>
        public static async Task<PipedResult> Finally<T>(this Task<PipedResult<T>> result, Func<Task> final)
        {
            var pipedResult = await result;
            if (!pipedResult.IsSuccess) return pipedResult;

            return await ExecuteAsync(final);
        }

        /// <summary>
        /// Enqueues a failure handler method to execute should the preceding method fail.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="final">The method to execute.</param>
        public static async Task<PipedResult> Finally(this Task<PipedResult> result, Func<Task> final)
        {
            var pipedResult = await result; 
            if (!pipedResult.IsSuccess) return pipedResult;

            return await ExecuteAsync(final);
        }




        private static async Task TestAsync()
        {
            var first = () => Task.FromResult(100);
            await first
                .PipeAsync(() => Task.CompletedTask)
                .OnFailAsync(r => Task.FromResult(r))
                .PipeAsync(() => Task.FromResult(4 * 2))
                .OnFailAsync(value => Task.FromResult(value))
                .PipeAsync(value => Task.FromResult("a string"))
                .PipeAsync(stringValue => Task.FromResult(stringValue + "xyz"))
                .PipeAsync(v => Task.CompletedTask)
                .PipeAsync(() => Task.CompletedTask)
                .PipeAsync(() => Task.FromResult(2))
                .PipeAsync(x => Task.FromResult(x))
                .Finally(() => Task.CompletedTask);

            var firstEmpty = () => Task.CompletedTask;
            var two = await firstEmpty
                .PipeAsync(() => Task.CompletedTask)
                .PipeAsync(() => Task.FromResult(4 * 2))
                .PipeAsync(value => Task.FromResult("a string"))
                .PipeAsync(stringValue => Task.FromResult(stringValue + "xyz"));
        }

        private static async Task<PipedResult> ExecuteAsync(Func<Task> asyncOperation)
        {
            try
            {
                await asyncOperation();
                return PipedResult.Success;
            }
            catch
            {
                return PipedResult.Fail();
            }

        }

        private static async Task<PipedResult<T>> ExecuteAsync<T>(Func<Task<T>> asyncOperation)
        {
            try
            {
                var result = await asyncOperation();
                return PipedResult<T>.Success(result);
            }
            catch
            {
                return PipedResult<T>.Fail();
            }
        }

        private static async Task<PipedResult<TOut>> ExecuteAsync<TIn, TOut>(Func<TIn, Task<TOut>> asyncOperation, TIn data)
        {
            try
            {
                var result = await asyncOperation(data);
                return PipedResult<TOut>.Success(result);
            }
            catch
            {
                return PipedResult<TOut>.Fail();
            }
        }

        private static async Task<PipedResult> ExecuteAsync<T>(Func<T, Task> asyncOperation, T data)
        {
            try
            {
                await asyncOperation(data);
                return PipedResult.Success;
            }
            catch
            {
                return PipedResult.Fail();
            }
        }

        private static async Task<PipedResult<T>> ExecutePassThroughAsync<T>(Func<PipedResult<T>, Task<PipedResult<T>>> asyncOperation, PipedResult<T> data)
        {
            try
            {
                return await asyncOperation(data);
            }
            catch
            {
                return PipedResult<T>.Fail();
            }
        }

        private static async Task<PipedResult> ExecutePassThroughAsync(Func<PipedResult, Task<PipedResult>> asyncOperation, PipedResult data)
        {
            try
            {
                return await asyncOperation(data);
            }
            catch
            {
                return PipedResult.Fail();
            }
        }
    }
}
