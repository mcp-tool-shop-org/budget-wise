using System.Text.Json;
using BudgetWise.Application.Interfaces;

namespace BudgetWise.Infrastructure.Web3;

public sealed class XrplClient : IXrplClient
{
    private readonly IWeb3Client _rpc;

    public XrplClient(IWeb3Client rpc)
    {
        _rpc = rpc ?? throw new ArgumentNullException(nameof(rpc));
    }

    public Task<Web3RpcResponse<JsonElement>> GetServerInfoAsync(CancellationToken ct = default)
        => _rpc.CallAsync<JsonElement>("server_info", new object?[] { new { } }, ct);

    public Task<Web3RpcResponse<JsonElement>> GetAccountInfoAsync(
        string accountAddress,
        bool strict = true,
        string ledgerIndex = "validated",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accountAddress))
            throw new ArgumentException("Account address is required.", nameof(accountAddress));

        var args = new
        {
            account = accountAddress,
            strict,
            ledger_index = ledgerIndex
        };

        return _rpc.CallAsync<JsonElement>("account_info", new object?[] { args }, ct);
    }
}
