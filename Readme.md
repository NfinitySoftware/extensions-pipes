## Nfinity Extensions Pipes

- [What is Extensions.Pipes?](#what-is-extensionspipes)
- [Why use Extensions.Pipes](#why-use-extensionspipes)?
- [How can I use Extensions.Pipes](#using-extensionspipes)?
- [License](#license)

### What is Extensions.Pipes?
*Extensions.Pipes* provides a mechanism for asynchronous pipeline processing in .NET, allowing multiple method calls to be combined into a single pipeline. This is usually provided by the `|` operator in many functional languages, but *Extensions.Pipes* provides a mechanism reminiscent of LINQ to achieve the same.

#### Main features:
- Method calls can be chained. The invocation of each method is dependent on the success of its predecessor.
- Piped methods can be followed by piped failure actions, which are automatically invoked should its associated method fail.
- The behavior of the invocation of pipeline failure actions can be customised. [See the `PipeFailureBehavior` enumeration.](#pipefailurebehavior)
- A `finally` action can be specified, completing a *try-catch-finally* construct in the pipeline, and which is always invoked whether pipeline actions succeed or fail.
- An aggregate result is made available as the result of the pipeline, and conditional code can be written based on its overall success or failure.
- State of the aggregate of failures is also available, in the case that any operation in the pipeline failed.

### Why use Extensions.Pipes?
- Massively reduce the amount of code required to reliably execute multiple methods in succession, where each is dependent on the success of its predecessor.
- Simplify conditional code based on the individual results of multiple operations. This is done naturally via the pipeline mechanism, and also by using the aggregate result of the pipeline once complete. This result indicates overall success.
- Maintain clean code principles, especially in complex scenario method chaining.

### Using Extensions.Pipes
Methods invoked via Extensions.Pipes are based on either:
-  Any awaitable method that returns `Task`,
-  Use of the `OperationResult` type, where chained methods return `Task<OperationResult>`. Use this structure when more specific, richer information is needed about the success or failure of methods.

Methods chained in the pipeline should follow a logical order and progression. Typically, starting a pipeline is done via the `AsyncPipe.Start` method, followed by multiple `PipeAsync` calls, and perhaps a call to `Finally`. Any failure actions, specified via `OnFailAsync`, should be chained directly after the piped method they are intended to handle. See the first example below.

#### `PipeFailureBehavior`

The `PipeFailureBehavior` enumeration determines how failed actions are run in the pipeline, or rather *which* are run. There are two options:

- `FailLastOnly`: The default behavior. Only the failure action associated with the last failed method will be run. In the first example below, if *SetUserSecurityAsync* failed, only *DeleteUserSecurityAsync* would be called.
- `FailAll`: All failure actions up the stack will be called in succession (from the last to the first). Again, in the first example below, if *SetUserSecurityAsync* failed, *DeleteUserSecurityAsync* would be called, then *DeleteUserAsync*.

#### Examples:
```csharp
public async Task ExecutePipelineAsync(string userName, string fullName, string email)
{
    //Example 1
    var result = await AsyncPipe
        .Start(() => CreateUserAsync(userName, fullName, email), PipeFailureBehavior.FailAll)
        .OnFailAsync(() => DeleteUserAsync(userName))
        .PipeAsync(() => SetUserSecurityAsync(userName, fullName, email))
        .OnFailAsync(() => DeleteUserSecurityAsync(userName))
        .Finally(() => CleanupTemporaryState(userName, fullName, email));

    //Example 2. It's also possible to specify one failure action for a pipeline, 
    //which will be called if any action fails.
    result = await AsyncPipe
        .Start(() => CreateUserAsync(userName, fullName, email))
        .PipeAsync(() => SetUserSecurityAsync(userName, fullName, email))
        .OnFailAsync(() => DeleteUserSecurityAsync(userName))
        .Finally(() => CleanupTemporaryState(userName, fullName, email));

    //Example 3. Could also declare the first action as a variable and use 
    //the extension methods to create the pipeline.
    var createUserAction = () => CreateUserAsync(userName, fullName, email);
    result = await createUserAction
        .PipeAsync(() => SetUserSecurityAsync(userName, fullName, email))
        .OnFailAsync(() => DeleteUserSecurityAsync(userName));

    //Example 4. It's also possible for each method to receive the result of its antededent.
    //OperationResult has a Data property, which also allows passing of state to each method.
    result = await AsyncPipe
        .Start(() => CreateUserAsync(userName, fullName, email), PipeFailureBehavior.FailAll)
        .PipeAsync(antecedent => SetUserSecurityAsync(userName, fullName, email, antecedent))
        .Finally(() => CleanupTemporaryState(userName, fullName, email));
    
    //Example 5. A pipeline using methods that return Task. No antecedent is available 
    //in this case.
    var taskResult = await AsyncPipe
        .Start(() => CreateSimpleUserAsync(userName, email))
        .PipeAsync(() => SetSimpleUserSecurityAsync(userName, email));

    //Conditional code based on the aggregated result
    if (!result.IsSuccess())
    {
        var failureState = result.GetFailureState();
        if (failureState.HttpStatusCode == System.Net.HttpStatusCode.Conflict)
        {
            //do something, say, when a user already exists by the given username
        }
    }
}

private Task<OperationResult> CreateUserAsync(string userName, string fullName, string email)
    => Task.FromResult(OperationResult.Success());

private Task<OperationResult> SetUserSecurityAsync(string userName, string fullName, string email)
    => Task.FromResult(OperationResult.Fail("Saving user security failed."));

private Task<OperationResult> SetUserSecurityAsync(string userName, string fullName, string email, 
    OperationResult antecedentResult)
{
    //do something with antecedent result
    return Task.FromResult(OperationResult.Success());
}

private Task<OperationResult> CleanupTemporaryState(string userName, string fullName, string email)
    => Task.FromResult(OperationResult.Success());

private Task<OperationResult> DeleteUserAsync(string userName)
    => Task.FromResult(OperationResult.Success());

private Task<OperationResult> DeleteUserSecurityAsync(string userName)
    => Task.FromResult(OperationResult.Success());

private Task CreateSimpleUserAsync(string userName, string email)
    => Task.FromResult(OperationResult.Success());

private Task SetSimpleUserSecurityAsync(string userName, string email)
    => Task.FromResult(OperationResult.Success());
```


### License
Nfinity.Extensions.Pipes is licensed under the [MIT](LICENSE.txt) license.