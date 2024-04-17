using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nfinity.Extensions.Pipes.Generic
{
    public static class GenericPipeExtensions
    {
        /// <summary>
        /// An alternative method for starting a pipeline.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedResult<TOut>> PipeAsync<TIn, TOut>(this Func<Task<TIn>> first, Func<TIn, Task<TOut>> next)
        {
            var pipedResult = await ExecuteAsync(first);
            if (!pipedResult.IsSuccess) return new PipedResult<TOut> { IsSuccess = false };

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
            if (!pipedResult.IsSuccess) return PipedResult.Fail;

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
            if (!pipedResult.IsSuccess) return PipedResult.Fail;

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
            if (!pipedResult.IsSuccess) return new PipedResult<TOut> { IsSuccess = false };

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
            if (!pipedResult.IsSuccess) return PipedResult.Fail;

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
            if (!pipedResult.IsSuccess) return new PipedResult<T> { IsSuccess = false };

            return await ExecuteAsync(next);
        }

        /// <summary>
        /// Enqueues a method in the pipeline.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedResult<T>> PipeAsync<T>(this Task result, Func<Task<T>> next)
        {
            try
            {
                await result;
            }
            catch
            {
                return new PipedResult<T> { IsSuccess = false };
            }

            var nextResult = await ExecuteAsync(next);
            return nextResult;
        }

        private static async Task TestAsync()
        {
            var first = () => Task.FromResult(100);
            await first
                .PipeAsync(() => Task.CompletedTask)
                .PipeAsync(() => Task.FromResult(4 * 2))
                .PipeAsync(value => Task.FromResult("a string"))
                .PipeAsync(stringValue => Task.FromResult(stringValue + "xyz"))
                .PipeAsync(() => Task.CompletedTask)
                .PipeAsync(() => Task.CompletedTask)
                .PipeAsync(() => Task.FromResult(2));

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
                return PipedResult.Fail;
            }

        }
        private static async Task<PipedResult<T>> ExecuteAsync<T>(Func<Task<T>> asyncOperation)
        {
            try
            {
                var result = await asyncOperation();
                return new PipedResult<T> { IsSuccess = true, Data = result };
            }
            catch
            {
                return new PipedResult<T> { IsSuccess = false };
            }
        }

        private static async Task<PipedResult<TOut>> ExecuteAsync<TIn, TOut>(Func<TIn, Task<TOut>> asyncOperation, TIn data)
        {
            try
            {
                var result = await asyncOperation(data);
                return new PipedResult<TOut> { IsSuccess = true, Data = result };
            }
            catch
            {
                return new PipedResult<TOut> { IsSuccess = false };
            }
        }
    }
}
