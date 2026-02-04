using System.Text.Json;

namespace NextLedger.Application.Interfaces;

public interface IXrplClient
{
    Task<Web3RpcResponse<JsonElement>> GetServerInfoAsync(CancellationToken ct = default);

    Task<Web3RpcResponse<JsonElement>> GetAccountInfoAsync(
        string accountAddress,
        bool strict = true,
        string ledgerIndex = "validated",
        CancellationToken ct = default);
}
