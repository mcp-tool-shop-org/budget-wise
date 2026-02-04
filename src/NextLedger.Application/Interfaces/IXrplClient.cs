using System.Text.Json;
using NextLedger.Application.DTOs;

namespace NextLedger.Application.Interfaces;

/// <summary>
/// Client for interacting with the XRP Ledger.
/// Read-only observation only - no signing, no submitting, no private keys.
/// </summary>
public interface IXrplClient
{
    /// <summary>
    /// Gets raw server info from the XRPL node.
    /// </summary>
    Task<Web3RpcResponse<JsonElement>> GetServerInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets raw account info for an XRPL address.
    /// </summary>
    Task<Web3RpcResponse<JsonElement>> GetAccountInfoAsync(
        string accountAddress,
        bool strict = true,
        string ledgerIndex = "validated",
        CancellationToken ct = default);

    /// <summary>
    /// Gets typed account info for an XRPL address.
    /// </summary>
    /// <param name="address">The XRPL r-address (public address only).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Typed account info result.</returns>
    Task<XrplAccountInfoResult> GetAccountInfoTypedAsync(string address, CancellationToken ct = default);

    /// <summary>
    /// Gets the current network status.
    /// </summary>
    Task<XrplNetworkStatus> GetNetworkStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Calculates the reserve for an account based on current network parameters.
    /// </summary>
    /// <param name="ownerCount">Number of owned objects (trust lines, offers, etc.).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<XrplReserveInfo> CalculateReserveAsync(int ownerCount, CancellationToken ct = default);

    /// <summary>
    /// Validates an XRPL address format (does NOT verify it exists on-chain).
    /// </summary>
    /// <param name="address">The address to validate.</param>
    /// <returns>True if the address format is valid.</returns>
    bool ValidateAddressFormat(string address);

    /// <summary>
    /// Gets transaction history for an XRPL address. Read-only.
    /// Uses the account_tx RPC method to fetch validated transactions.
    /// </summary>
    /// <param name="request">Request parameters including address and pagination.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Transaction history result.</returns>
    Task<XrplTransactionHistoryResult> GetTransactionHistoryAsync(
        XrplTransactionHistoryRequest request,
        string address,
        CancellationToken ct = default);

    /// <summary>
    /// Compares the current on-chain balance with a cached value.
    /// Returns reconciliation status without making changes.
    /// </summary>
    /// <param name="address">XRPL address to check.</param>
    /// <param name="cachedBalanceDrops">NextLedger's cached balance in drops.</param>
    /// <param name="cachedSyncAt">When the cache was last updated.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<XrplReconciliationDto> GetReconciliationStatusAsync(
        string address,
        long cachedBalanceDrops,
        DateTime? cachedSyncAt,
        CancellationToken ct = default);
}
