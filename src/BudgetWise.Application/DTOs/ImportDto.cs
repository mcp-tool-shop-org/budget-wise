using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Application.DTOs;

public enum CsvImportRowStatus
{
    New = 0,
    Duplicate = 1,
    Invalid = 2
}

public sealed record CsvImportPreviewRequest
{
    public required Guid AccountId { get; init; }
    public required string CsvText { get; init; }
    public bool HasHeaderRow { get; init; } = true;
}

public sealed record CsvImportRowDto
{
    public required int RowNumber { get; init; }
    public DateOnly? Date { get; init; }
    public Money? Amount { get; init; }
    public required string Payee { get; init; }
    public string? Memo { get; init; }

    public required CsvImportRowStatus Status { get; init; }
    public string? Fingerprint { get; init; }
    public string? Error { get; init; }
}

public sealed record CsvImportPreviewResultDto
{
    public required Guid AccountId { get; init; }
    public DateOnly? MinDate { get; init; }
    public DateOnly? MaxDate { get; init; }

    public required int TotalRowCount { get; init; }
    public required int ParsedRowCount { get; init; }
    public required int NewCount { get; init; }
    public required int DuplicateCount { get; init; }
    public required int InvalidCount { get; init; }

    public required IReadOnlyList<CsvImportRowDto> Rows { get; init; }
}

public sealed record CsvImportCommitRequest
{
    public required Guid AccountId { get; init; }
    public required IReadOnlyList<CsvImportCommitRowDto> Rows { get; init; }
}

public sealed record CsvImportCommitRowDto
{
    public required int RowNumber { get; init; }
    public required DateOnly Date { get; init; }
    public required Money Amount { get; init; }
    public required string Payee { get; init; }
    public string? Memo { get; init; }
    public required string Fingerprint { get; init; }
}

public sealed record CsvImportCommitResultDto
{
    public required Guid AccountId { get; init; }
    public required int InsertedCount { get; init; }
    public required int SkippedDuplicateCount { get; init; }
}
