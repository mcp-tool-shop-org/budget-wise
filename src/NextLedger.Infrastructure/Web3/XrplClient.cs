using System.Text.Json;
using System.Text.RegularExpressions;
using NextLedger.Application.DTOs;
using NextLedger.Application.Interfaces;

namespace NextLedger.Infrastructure.Web3;

/// <summary>
/// Client for interacting with the XRP Ledger.
/// Read-only observation only - no signing, no submitting, no private keys.
/// </summary>
public sealed partial class XrplClient : IXrplClient
{
    private readonly IWeb3Client _rpc;

    // Default reserve values (can be overridden by server_info)
    private const long DefaultBaseReserveDrops = 10_000_000; // 10 XRP
    private const long DefaultOwnerReserveDrops = 2_000_000; // 2 XRP

    // Cache the last known reserve values
    private long _cachedBaseReserve = DefaultBaseReserveDrops;
    private long _cachedOwnerReserve = DefaultOwnerReserveDrops;

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

    public async Task<XrplAccountInfoResult> GetAccountInfoTypedAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return XrplAccountInfoResult.Fail(address ?? "", "Address is required.");

        if (!ValidateAddressFormat(address))
            return XrplAccountInfoResult.Fail(address, "Invalid XRPL address format.");

        try
        {
            var response = await GetAccountInfoAsync(address, strict: true, "validated", ct);

            if (!response.Success || response.Error is not null)
            {
                var errorMsg = response.Error?.Message ?? "Unknown error";
                // Check for common XRPL error codes
                if (errorMsg.Contains("actNotFound", StringComparison.OrdinalIgnoreCase))
                    return XrplAccountInfoResult.Fail(address, "Account not found. The address may not be activated (requires minimum XRP deposit).");

                return XrplAccountInfoResult.Fail(address, errorMsg);
            }

            var result = response.Result;
            if (result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null)
                return XrplAccountInfoResult.Fail(address, "Empty response from XRPL node.");

            // Parse the account_data from response
            if (!result.TryGetProperty("account_data", out var accountData))
                return XrplAccountInfoResult.Fail(address, "No account_data in response.");

            // Extract balance (in drops as string)
            var balanceDrops = 0L;
            if (accountData.TryGetProperty("Balance", out var balanceProp))
            {
                var balanceStr = balanceProp.GetString();
                if (!string.IsNullOrEmpty(balanceStr) && long.TryParse(balanceStr, out var parsed))
                    balanceDrops = parsed;
            }

            // Extract owner count
            var ownerCount = 0;
            if (accountData.TryGetProperty("OwnerCount", out var ownerProp))
                ownerCount = ownerProp.GetInt32();

            // Extract sequence
            var sequence = 0L;
            if (accountData.TryGetProperty("Sequence", out var seqProp))
                sequence = seqProp.GetInt64();

            // Extract ledger index
            var ledgerIndex = 0L;
            if (result.TryGetProperty("ledger_index", out var ledgerProp))
                ledgerIndex = ledgerProp.GetInt64();
            else if (result.TryGetProperty("ledger_current_index", out var currentProp))
                ledgerIndex = currentProp.GetInt64();

            return XrplAccountInfoResult.Ok(address, balanceDrops, ownerCount, sequence, ledgerIndex);
        }
        catch (Exception ex)
        {
            return XrplAccountInfoResult.Fail(address, $"Failed to fetch account info: {ex.Message}");
        }
    }

    public async Task<XrplNetworkStatus> GetNetworkStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await GetServerInfoAsync(ct);

            if (!response.Success || response.Error is not null)
            {
                var errorMsg = response.Error?.Message ?? "Unable to reach XRPL node.";
                return XrplNetworkStatus.Disconnected(errorMsg);
            }

            var result = response.Result;
            if (result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null)
                return XrplNetworkStatus.Disconnected("Empty response from XRPL node.");

            // Navigate to info object
            if (!result.TryGetProperty("info", out var info))
                return XrplNetworkStatus.Disconnected("No 'info' in server_info response.");

            // Extract network details
            string? networkId = null;
            if (info.TryGetProperty("network_id", out var netIdProp))
                networkId = netIdProp.ToString();

            string? serverVersion = null;
            if (info.TryGetProperty("build_version", out var versionProp))
                serverVersion = versionProp.GetString();

            var validatedLedger = 0L;
            if (info.TryGetProperty("validated_ledger", out var validatedProp) &&
                validatedProp.TryGetProperty("seq", out var seqProp))
            {
                validatedLedger = seqProp.GetInt64();
            }

            // Extract reserve values
            var baseReserve = DefaultBaseReserveDrops;
            var ownerReserve = DefaultOwnerReserveDrops;

            if (info.TryGetProperty("validated_ledger", out var ledgerInfo))
            {
                if (ledgerInfo.TryGetProperty("reserve_base_xrp", out var baseXrp))
                    baseReserve = (long)(baseXrp.GetDecimal() * 1_000_000);

                if (ledgerInfo.TryGetProperty("reserve_inc_xrp", out var incXrp))
                    ownerReserve = (long)(incXrp.GetDecimal() * 1_000_000);
            }

            // Cache the reserve values
            _cachedBaseReserve = baseReserve;
            _cachedOwnerReserve = ownerReserve;

            // Get endpoint from options (we don't have direct access, so return placeholder)
            var endpoint = "XRPL JSON-RPC";

            return XrplNetworkStatus.Connected(
                endpoint,
                networkId,
                serverVersion,
                validatedLedger,
                baseReserve,
                ownerReserve);
        }
        catch (Exception ex)
        {
            return XrplNetworkStatus.Disconnected($"Failed to connect: {ex.Message}");
        }
    }

    public Task<XrplReserveInfo> CalculateReserveAsync(int ownerCount, CancellationToken ct = default)
    {
        // Use cached values (updated by GetNetworkStatusAsync)
        var result = new XrplReserveInfo
        {
            BaseReserveDrops = _cachedBaseReserve,
            OwnerReserveDrops = _cachedOwnerReserve,
            OwnerCount = ownerCount
        };

        return Task.FromResult(result);
    }

    public bool ValidateAddressFormat(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return false;

        // XRPL addresses:
        // - Start with 'r'
        // - Are 25-35 characters long
        // - Use base58 alphabet (excludes 0, O, I, l)
        // - Have a valid checksum (we do basic validation only, not full checksum)

        if (address.Length < 25 || address.Length > 35)
            return false;

        if (!address.StartsWith('r'))
            return false;

        // Check for valid base58 characters (no 0, O, I, l)
        return XrplAddressRegex().IsMatch(address);
    }

    [GeneratedRegex(@"^r[1-9A-HJ-NP-Za-km-z]{24,34}$")]
    private static partial Regex XrplAddressRegex();
}
