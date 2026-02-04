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
}
