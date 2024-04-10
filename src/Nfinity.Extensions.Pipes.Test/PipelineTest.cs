using System.Net;

namespace Nfinity.Extensions.Pipes.Test
{
    [TestClass]
    public class PipelineTest
    {
        private const string _exceptionMessage = "Manual exception";
        private const string _opFailedMessage = "Operation failed";

        [TestMethod]
        public async Task PipeAsync_TwoActions_Success()
        {
            var firstResult = 0;
            var secondResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult));

            Assert.IsTrue(result.IsSuccess());
            Assert.AreEqual(6, firstResult);
            Assert.AreEqual(10, secondResult);
        }

        [TestMethod]
        public async Task PipeAsync_Many_FailFirst_DefaultBehaviour()
        {
            var firstResult = 0;
            var secondResult = 0;
            var thirdResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, fail: true))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult))
                .PipeAsync(() => AddAsync(4, 8, ref thirdResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(1, result.Results.Count, "Expected only the successful results plus the last failed result to be present");
            Assert.AreEqual(0, firstResult);
            Assert.AreEqual(0, secondResult);
            Assert.AreEqual(0, thirdResult);
        }

        [TestMethod]
        public async Task PipeAsync_Many_ExceptionOnFirst_DefaultBehaviour()
        {
            var firstResult = 0;
            var secondResult = 0;
            var thirdResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, throwException: true))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult))
                .PipeAsync(() => AddAsync(4, 8, ref thirdResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(1, result.Results.Count, "Expected only the successful results plus the last failed result to be present");
            Assert.AreEqual(0, firstResult);
            Assert.AreEqual(0, secondResult);
            Assert.AreEqual(0, thirdResult);
        }

        [TestMethod]
        public async Task PipeAsync_Many_Fail_MidPipeline_DefaultBehaviour()
        {
            var firstResult = 0;
            var secondResult = 0;
            var thirdResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult, fail: true))
                .PipeAsync(() => AddAsync(4, 8, ref thirdResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(2, result.Results.Count, "Expected only the successful results plus the last failed result to be present");
            Assert.AreEqual(6, firstResult);
            Assert.AreEqual(0, secondResult);
            Assert.AreEqual(0, thirdResult);
        }

        [TestMethod]
        public async Task PipeAsync_Many_Exception_MidPipeline_DefaultBehaviour()
        {
            var firstResult = 0;
            var secondResult = 0;
            var thirdResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult, throwException: true))
                .PipeAsync(() => AddAsync(4, 8, ref thirdResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(2, result.Results.Count, "Expected only the successful results plus the last failed result to be present");
            Assert.AreEqual(6, firstResult);
            Assert.AreEqual(0, secondResult);
            Assert.AreEqual(0, thirdResult);
        }

        [TestMethod]
        public async Task PipeAsync_One_Fail_WithFailAction()
        {
            var firstResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, fail: true))
                .OnFailAsync(() => ResetAsync(0, ref firstResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(1, result.Results.Count);

            Assert.IsNotNull(result.FailActionResults);
            Assert.IsNotNull(result.FailActionTasks);
            Assert.AreEqual(1, result.FailActionResults.Count);
            Assert.AreEqual(1, result.FailActionTasks.Count);
            
            Assert.AreEqual(0, firstResult, "Expected the fail action to reset the value");
        }

        [TestMethod]
        public async Task PipeAsync_One_Exception_WithFailAction()
        {
            var firstResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, throwException: true))
                .OnFailAsync(() => ResetAsync(0, ref firstResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(1, result.Results.Count);

            Assert.IsNotNull(result.FailActionResults);
            Assert.IsNotNull(result.FailActionTasks);
            Assert.AreEqual(1, result.FailActionResults.Count);
            Assert.AreEqual(1, result.FailActionTasks.Count);

            Assert.AreEqual(0, firstResult, "Expected the fail action to reset the value");
        }

        [TestMethod]
        public async Task PipeAsync_Many_FailLastAction()
        {
            var firstResult = 0;
            var secondResult = 0;
            var thirdResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult))
                .OnFailAsync(() => ResetAsync(0, ref firstResult))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult))
                .OnFailAsync(() => ResetAsync(0, ref secondResult))
                .PipeAsync(() => AddAsync(4, 8, ref thirdResult, throwException: true))
                .OnFailAsync(() => ResetAsync(0, ref thirdResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(3, result.Results.Count);

            Assert.AreEqual(3, result.FailActions.Count);
            Assert.IsNotNull(result.FailActionResults);
            Assert.IsNotNull(result.FailActionTasks);
            Assert.AreEqual(1, result.FailActionResults.Count);
            Assert.AreEqual(1, result.FailActionTasks.Count);

            Assert.AreEqual(6, firstResult);
            Assert.AreEqual(10, secondResult);
            Assert.AreEqual(0, thirdResult, "Expected the fail action to reset the value");
        }

        [TestMethod]
        public async Task PipeAsync_Many_ThrowException_FailEntireStack()
        {
            var firstResult = 0;
            var secondResult = 0;
            var thirdResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult), PipeFailureBehavior.FailAll)
                .OnFailAsync(() => ResetAsync(0, ref firstResult))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult))
                .OnFailAsync(() => ResetAsync(0, ref secondResult))
                .PipeAsync(() => AddAsync(4, 8, ref thirdResult, throwException: true))
                .OnFailAsync(() => ResetAsync(0, ref thirdResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(3, result.Results.Count);

            Assert.AreEqual(3, result.FailActions.Count);
            Assert.IsNotNull(result.FailActionResults);
            Assert.IsNotNull(result.FailActionTasks);
            Assert.AreEqual(3, result.FailActionResults.Count);
            Assert.AreEqual(3, result.FailActionTasks.Count);

            Assert.AreEqual(0, firstResult, "Expected the fail action to reset the value");
            Assert.AreEqual(0, secondResult, "Expected the fail action to reset the value");
            Assert.AreEqual(0, thirdResult, "Expected the fail action to reset the value");
        }

        [TestMethod]
        public async Task PipeAsync_Many_ThrowException_FailStack_MidPipeline()
        {
            var firstResult = 0;
            var secondResult = 0;
            var thirdResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult), PipeFailureBehavior.FailAll)
                .OnFailAsync(() => ResetAsync(0, ref firstResult))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult, fail: true))
                .OnFailAsync(() => ResetAsync(0, ref secondResult))
                .PipeAsync(() => AddAsync(4, 8, ref thirdResult))
                .OnFailAsync(() => ResetAsync(0, ref thirdResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(2, result.Results.Count);

            Assert.AreEqual(3, result.FailActions.Count);
            Assert.IsNotNull(result.FailActionResults);
            Assert.IsNotNull(result.FailActionTasks);
            Assert.AreEqual(2, result.FailActionResults.Count);
            Assert.AreEqual(2, result.FailActionTasks.Count);

            Assert.AreEqual(0, firstResult, "Expected the fail action to reset the value");
            Assert.AreEqual(0, secondResult, "Expected the fail action to reset the value");
            Assert.AreEqual(0, thirdResult, "Expected this not to be set");
        }

        [TestMethod]
        public async Task PipeAsync_Many_Failure_WithFinally()
        {
            var firstResult = 0;
            var secondResult = 0;
            var finallyResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult))
                .OnFailAsync(() => ResetAsync(0, ref firstResult))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult, throwException: true))
                .OnFailAsync(() => ResetAsync(0, ref secondResult))
                .Finally(() => AddAsync(4, 8, ref finallyResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(2, result.Results.Count);

            Assert.AreEqual(2, result.FailActions.Count);
            Assert.IsNotNull(result.FailActionResults);
            Assert.IsNotNull(result.FailActionTasks);
            Assert.AreEqual(1, result.FailActionResults.Count);
            Assert.AreEqual(1, result.FailActionTasks.Count);

            Assert.AreEqual(6, firstResult);
            Assert.AreEqual(0, secondResult, "Expected the fail action to reset the value");
            Assert.AreEqual(12, finallyResult, "Expected the finally to always run");
        }

        [TestMethod]
        public async Task PipeAsync_Many_WithFinally()
        {
            var firstResult = 0;
            var secondResult = 0;
            var finallyResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult))
                .OnFailAsync(() => ResetAsync(0, ref firstResult))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult))
                .OnFailAsync(() => ResetAsync(0, ref secondResult))
                .Finally(() => AddAsync(4, 8, ref finallyResult));

            Assert.IsTrue(result.IsSuccess());
            Assert.AreEqual(2, result.Results.Count);

            Assert.AreEqual(2, result.FailActions.Count);
            Assert.IsNull(result.FailActionResults);
            Assert.IsNull(result.FailActionTasks);

            Assert.AreEqual(6, firstResult);
            Assert.AreEqual(10, secondResult);
            Assert.AreEqual(12, finallyResult, "Expected the finally to always run");
        }

        [TestMethod]
        public async Task PipeAsync_Many_WithFinally_OutOfOrder()
        {
            var firstResult = 0;
            var secondResult = 0;
            var finallyResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult))
                .OnFailAsync(() => ResetAsync(0, ref firstResult))
                .Finally(() => AddAsync(4, 8, ref finallyResult))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult))
                .OnFailAsync(() => ResetAsync(0, ref secondResult));

            Assert.IsTrue(result.IsSuccess());
            Assert.AreEqual(2, result.Results.Count);

            Assert.AreEqual(2, result.FailActions.Count);
            Assert.IsNull(result.FailActionResults);
            Assert.IsNull(result.FailActionTasks);

            Assert.AreEqual(6, firstResult);
            Assert.AreEqual(10, secondResult);
            Assert.AreEqual(12, finallyResult, "Expected the finally to always run");
        }

        [TestMethod]
        public async Task PipeAsync_GetFailureState_NoneFailed()
        {
            var firstResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult))
                .OnFailAsync(() => ResetAsync(0, ref firstResult));

            var state = result.GetFailureState();
            Assert.IsNull(state);
        }

        [TestMethod]
        public async Task PipeAsync_GetFailureState_FailFirst()
        {
            var firstResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, fail: true, failureStatusCode: HttpStatusCode.BadRequest))
                .OnFailAsync(() => ResetAsync(0, ref firstResult));

            var state = result.GetFailureState();
            Assert.IsNotNull(state);
            Assert.AreEqual(HttpStatusCode.BadRequest, state.HttpStatusCode);
            Assert.IsNotNull(state.Exception);
            Assert.AreEqual(_opFailedMessage, state.FailureReason);
        }

        [TestMethod]
        public async Task PipeAsync_GetFailureState_FailAll()
        {
            var firstResult = 0;
            var secondResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult), PipeFailureBehavior.FailAll)
                .OnFailAsync(() => ResetAsync(0, ref firstResult))
                .PipeAsync(() => AddAsync(3, 7, ref secondResult, fail: true, failureStatusCode: HttpStatusCode.BadRequest))
                .OnFailAsync(() => ResetAsync(0, ref secondResult));

            var state = result.GetFailureState();
            Assert.IsNotNull(state);
            Assert.AreEqual(HttpStatusCode.BadRequest, state.HttpStatusCode);
            Assert.IsNotNull(state.Exception);
            Assert.IsInstanceOfType<AggregateException>(state.Exception);
            Assert.AreEqual(1, ((AggregateException)state.Exception).InnerExceptions.Count);
            Assert.AreEqual(_opFailedMessage, state.FailureReason);
        }

        private static Task<OperationResult> AddAsync(int x, int y, ref int result, bool fail = false, bool throwException = false,
            HttpStatusCode failureStatusCode = HttpStatusCode.InternalServerError)
        {
            if (throwException) throw new HttpRequestException(_exceptionMessage, new Exception(_exceptionMessage), failureStatusCode);
            if (fail) return Task.FromResult(OperationResult.Fail(_opFailedMessage, new Exception(_exceptionMessage), statusCode: failureStatusCode));

            result = x + y;
            return Task.FromResult(OperationResult.Success());
        }

        private static Task ResetAsync(int value, ref int result)
        {
            result = value;
            return Task.CompletedTask;
        }
    }
}
