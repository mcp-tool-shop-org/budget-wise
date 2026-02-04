namespace BudgetWise.Infrastructure.Web3;

public sealed record Web3Options
{
    public string RpcUrl { get; init; } = string.Empty;
}
