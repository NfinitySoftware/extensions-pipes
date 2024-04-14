using System.Net;

namespace Nfinity.Extensions.Pipes.Test
{
    [TestClass]
    public class PipelineForwardResultTest
    {
        private const string ExceptionMessage = "Manual exception";
        private const string OpFailedMessage = "Operation failed";

        [TestMethod]
        public async Task PipeAsync_TwoActions_Success()
        {
            var firstResult = 0;
            var secondResult = 0;
            var data = new object();

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, data: data))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult, expectedAntecedentData: data));

            Assert.IsTrue(result.IsSuccess());
            Assert.AreEqual(6, firstResult);
            Assert.AreEqual(10, secondResult);
        }

        [TestMethod]
        public async Task PipeAsync_TwoActions_Success_WithData()
        {
            var firstResult = 0;
            var secondResult = 0;
            var data = new object();

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, data: data))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult, expectedAntecedentData: data));

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
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult))
                .PipeAsync(antecedent => AddAsync(antecedent, 4, 8, ref thirdResult));

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
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult))
                .PipeAsync(antecedent => AddAsync(antecedent, 4, 8, ref thirdResult));

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
            var data = new object();

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, data: data))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult, fail: true, expectedAntecedentData: data))
                .PipeAsync(antecedent => AddAsync(antecedent, 4, 8, ref thirdResult));

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
            var data = new object();

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, data: data))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult, throwException: true, expectedAntecedentData: data))
                .PipeAsync(antecedent => AddAsync(antecedent, 4, 8, ref thirdResult));

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
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult, expectAntecedentSuccess: false));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(1, result.Results.Count);

            Assert.IsNotNull(result.FailActionResults);
            Assert.AreEqual(1, result.FailActionResults.Count);
            Assert.IsTrue(result.FailActionResults.All(r => r.IsSuccess));

            Assert.AreEqual(0, firstResult, "Expected the fail action to reset the value");
        }

        [TestMethod]
        public async Task PipeAsync_One_Exception_WithFailAction()
        {
            var firstResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, throwException: true))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult, expectAntecedentSuccess: false));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(1, result.Results.Count);

            Assert.IsNotNull(result.FailActionResults);
            Assert.AreEqual(1, result.FailActionResults.Count);
            Assert.IsTrue(result.FailActionResults.All(r => r.IsSuccess));

            Assert.AreEqual(0, firstResult, "Expected the fail action to reset the value");
        }

        [TestMethod]
        public async Task PipeAsync_Many_FailLastAction()
        {
            var firstResult = 0;
            var secondResult = 0;
            var thirdResult = 0;
            var dataA = new object();
            var dataB = new object();

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, data: dataA))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult, data: dataB, expectedAntecedentData: dataA))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref secondResult))
                .PipeAsync(antecedent => AddAsync(antecedent, 4, 8, ref thirdResult, throwException: true, expectedAntecedentData: dataB))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref thirdResult, expectAntecedentSuccess: false));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(3, result.Results.Count);

            Assert.AreEqual(3, result.FailActions.Count);
            Assert.IsNotNull(result.FailActionResults);
            Assert.AreEqual(1, result.FailActionResults.Count);
            Assert.IsTrue(result.FailActionResults.All(r => r.IsSuccess));

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
            var dataA = new object();
            var dataB = new object();

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, data: dataA), PipeFailureBehavior.FailAll)
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult, expectedAntecedentData: dataA))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult, data: dataB, expectedAntecedentData: dataA))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref secondResult, expectedAntecedentData: dataB))
                .PipeAsync(antecedent => AddAsync(antecedent, 4, 8, ref thirdResult, throwException: true, expectedAntecedentData: dataB))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref thirdResult, expectAntecedentSuccess: false));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(3, result.Results.Count);

            Assert.AreEqual(3, result.FailActions.Count);
            Assert.IsNotNull(result.FailActionResults);
            Assert.AreEqual(3, result.FailActionResults.Count);
            Assert.IsTrue(result.FailActionResults.All(r => r.IsSuccess));

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
            var data = new object();

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, data: data), PipeFailureBehavior.FailAll)
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult, expectedAntecedentData: data))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult, fail: true, expectedAntecedentData: data))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref secondResult, expectAntecedentSuccess: false))
                .PipeAsync(antecedent => AddAsync(antecedent, 4, 8, ref thirdResult))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref thirdResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(2, result.Results.Count);

            Assert.AreEqual(2, result.FailActions.Count);
            Assert.IsNotNull(result.FailActionResults);
            Assert.AreEqual(2, result.FailActionResults.Count);
            Assert.IsTrue(result.FailActionResults.All(r => r.IsSuccess));

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
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult, throwException: true))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref secondResult, expectAntecedentSuccess: false))
                .Finally(() => AddAsync(4, 8, ref finallyResult));

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(2, result.Results.Count);

            Assert.AreEqual(2, result.FailActions.Count);
            Assert.IsNotNull(result.FailActionResults);
            Assert.AreEqual(1, result.FailActionResults.Count);
            Assert.IsTrue(result.FailActionResults.All(r => r.IsSuccess));

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
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref secondResult))
                .Finally(() => AddAsync(4, 8, ref finallyResult));

            Assert.IsTrue(result.IsSuccess());
            Assert.AreEqual(2, result.Results.Count);

            Assert.AreEqual(2, result.FailActions.Count);
            Assert.IsNull(result.FailActionResults);

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
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult));

            var state = result.GetFailureState();
            Assert.IsNull(state);
        }

        [TestMethod]
        public async Task PipeAsync_GetFailureState_FailFirst()
        {
            var firstResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult, fail: true, failureStatusCode: HttpStatusCode.BadRequest))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult));

            var state = result.GetFailureState();
            Assert.IsNotNull(state);
            Assert.AreEqual(HttpStatusCode.BadRequest, state.HttpStatusCode);
            Assert.IsNotNull(state.Exception);
            Assert.AreEqual(OpFailedMessage, state.FailureReason);
        }

        [TestMethod]
        public async Task PipeAsync_GetFailureState_FailAll()
        {
            var firstResult = 0;
            var secondResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult), PipeFailureBehavior.FailAll)
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult, fail: true, failureStatusCode: HttpStatusCode.BadRequest))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref secondResult, expectAntecedentSuccess: false));

            var state = result.GetFailureState();
            Assert.IsNotNull(state);
            Assert.AreEqual(HttpStatusCode.BadRequest, state.HttpStatusCode);
            Assert.IsNotNull(state.Exception);
            Assert.AreEqual(1, state.Exception.InnerExceptions.Count);
            Assert.AreEqual(OpFailedMessage, state.FailureReason);
        }

        [TestMethod]
        public async Task PipeAsync_GetFailureState_FailureActionsFail_FailAll()
        {
            var firstResult = 0;
            var secondResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult), PipeFailureBehavior.FailAll)
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult, throwException: true))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult, fail: true, failureStatusCode: HttpStatusCode.BadRequest))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref secondResult, throwException: true, expectAntecedentSuccess: false));

            var state = result.GetFailureState();
            Assert.IsNotNull(state);
            Assert.AreEqual(HttpStatusCode.BadRequest, state.HttpStatusCode);
            Assert.IsNotNull(state.Exception);
            Assert.AreEqual(3, state.Exception.InnerExceptions.Count);
            Assert.AreEqual(OpFailedMessage, state.FailureReason);
        }

        [TestMethod]
        public async Task PipeAsync_GetFailureState_FailureAndFinalActionsFail_FailAll()
        {
            var firstResult = 0;
            var secondResult = 0;
            var finallyResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult), PipeFailureBehavior.FailAll)
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult, throwException: true))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult, fail: true, failureStatusCode: HttpStatusCode.BadRequest))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref secondResult, throwException: true, expectAntecedentSuccess: false))
                .Finally(() => AddAsync(4, 8, ref finallyResult, throwException: true));

            var state = result.GetFailureState();
            Assert.IsNotNull(state);
            Assert.AreEqual(HttpStatusCode.BadRequest, state.HttpStatusCode);
            Assert.IsNotNull(state.Exception);
            Assert.AreEqual(4, state.Exception.InnerExceptions.Count);
            Assert.AreEqual(OpFailedMessage, state.FailureReason);
        }

        [TestMethod]
        public async Task PipeAsync_GetFailureState_OnlyFinallyFailed()
        {
            var firstResult = 0;
            var secondResult = 0;
            var finallyResult = 0;

            var result = await AsyncPipe
                .Start(() => AddAsync(2, 4, ref firstResult))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref firstResult))
                .PipeAsync(antecedent => AddAsync(antecedent, 3, 7, ref secondResult))
                .OnFailAsync(antecedent => ResetAsync(antecedent, 0, ref secondResult))
                .Finally(() => AddAsync(4, 8, ref finallyResult, throwException: true));

            Assert.IsFalse(result.IsSuccess());

            var state = result.GetFailureState();
            Assert.IsNotNull(state);
            Assert.IsNotNull(state.Exception);
            Assert.AreEqual(1, state.Exception.InnerExceptions.Count);
        }

        private static Task<OperationResult> AddAsync(int x, int y, ref int result, bool fail = false, bool throwException = false,
            HttpStatusCode failureStatusCode = HttpStatusCode.InternalServerError, object data = null)
        {
            if (throwException) throw new HttpRequestException(ExceptionMessage, new Exception(ExceptionMessage), failureStatusCode);
            if (fail) return Task.FromResult(OperationResult.Fail(new Exception(ExceptionMessage), OpFailedMessage, statusCode: failureStatusCode));

            result = x + y;
            return Task.FromResult(OperationResult.Success(data));
        }

        private static Task<OperationResult> AddAsync(OperationResult antecedentResult, int x, int y, ref int result, bool fail = false, bool throwException = false,
            HttpStatusCode failureStatusCode = HttpStatusCode.InternalServerError, object data = null, object expectedAntecedentData = null)
        {
            Assert.IsNotNull(antecedentResult);
            Assert.IsTrue(antecedentResult.IsSuccess);
            Assert.AreSame(expectedAntecedentData, antecedentResult.Data);

            if (throwException) throw new HttpRequestException(ExceptionMessage, new Exception(ExceptionMessage), failureStatusCode);
            if (fail) return Task.FromResult(OperationResult.Fail(new Exception(ExceptionMessage), OpFailedMessage, statusCode: failureStatusCode));

            result = x + y;
            return Task.FromResult(OperationResult.Success(data));
        }

        private static Task ResetAsync(OperationResult antecedentResult, int value, ref int result, bool throwException = false,
            bool expectAntecedentSuccess = true, object expectedAntecedentData = null)
        {
            Assert.IsNotNull(antecedentResult);
            Assert.AreEqual(expectAntecedentSuccess, antecedentResult.IsSuccess);
            Assert.AreSame(expectedAntecedentData, antecedentResult.Data);

            if (throwException) throw new HttpRequestException(ExceptionMessage, new Exception(ExceptionMessage));

            result = value;
            return Task.CompletedTask;
        }
    }
}
