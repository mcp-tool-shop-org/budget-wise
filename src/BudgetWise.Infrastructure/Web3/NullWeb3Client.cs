using BudgetWise.Application.Interfaces;

namespace BudgetWise.Infrastructure.Web3;

public sealed class NullWeb3Client : IWeb3Client
{
    public Task<Web3RpcResponse<T>> CallAsync<T>(string method, object?[]? @params = null, CancellationToken ct = default)
    {
        var error = new Web3RpcError(
            Code: -1,
            Message: "Web3 is not configured. Set BUDGETWISE_WEB3_RPC_URL (for XRPL this should be a rippled JSON-RPC URL).",
            Data: null);

        return Task.FromResult(new Web3RpcResponse<T>(Result: default, Error: error));
    }
}
