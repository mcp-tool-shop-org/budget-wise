namespace BudgetWise.Application.Interfaces;

public interface IWeb3Client
{
    Task<Web3RpcResponse<T>> CallAsync<T>(
        string method,
        object?[]? @params = null,
        CancellationToken ct = default);
}

public sealed record Web3RpcError(int Code, string Message, object? Data);

public sealed record Web3RpcResponse<T>(T? Result, Web3RpcError? Error)
{
    public bool Success => Error is null;
}
