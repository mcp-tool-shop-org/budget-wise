using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BudgetWise.Application.Interfaces;

namespace BudgetWise.Infrastructure.Web3;

public sealed class EthereumJsonRpcClient : IWeb3Client
{
    private readonly HttpClient _http;
    private readonly Uri _rpcUri;
    private long _nextId = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public EthereumJsonRpcClient(HttpClient http, Web3Options options)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));

        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.RpcUrl))
            throw new InvalidOperationException("Web3 RPC URL not configured. Set BUDGETWISE_WEB3_RPC_URL.");

        _rpcUri = new Uri(options.RpcUrl, UriKind.Absolute);
    }

    public async Task<Web3RpcResponse<T>> CallAsync<T>(string method, object?[]? @params = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("Method is required.", nameof(method));

        var request = new JsonRpcRequest(method, @params ?? Array.Empty<object?>(), Interlocked.Increment(ref _nextId));

        using var response = await _http.PostAsJsonAsync(_rpcUri, request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();

        var rpc = await response.Content.ReadFromJsonAsync<JsonRpcResponse<T>>(JsonOptions, ct);
        if (rpc is null)
            throw new InvalidOperationException("Invalid JSON-RPC response.");

        return new Web3RpcResponse<T>(rpc.Result, rpc.Error);
    }

    private sealed record JsonRpcRequest(
        string Method,
        object?[] Params,
        long Id)
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; init; } = "2.0";

        [JsonPropertyName("method")]
        public string Method { get; init; } = Method;

        [JsonPropertyName("params")]
        public object?[] Params { get; init; } = Params;

        [JsonPropertyName("id")]
        public long Id { get; init; } = Id;
    }

    private sealed record JsonRpcResponse<T>(
        [property: JsonPropertyName("result")] T? Result,
        [property: JsonPropertyName("error")] Web3RpcError? Error);
}
