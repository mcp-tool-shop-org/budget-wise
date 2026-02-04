using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BudgetWise.Application.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Application.Services;

public sealed class CsvImportService
{
    private static readonly string[] DateHeaderCandidates = ["date", "transaction date", "posted date"];
    private static readonly string[] PayeeHeaderCandidates = ["payee", "description", "name", "merchant", "transaction"];
    private static readonly string[] MemoHeaderCandidates = ["memo", "notes", "note", "details"];
    private static readonly string[] AmountHeaderCandidates = ["amount", "amt", "value"];
    private static readonly string[] DepositHeaderCandidates = ["deposit", "credit", "inflow", "paid in"];
    private static readonly string[] WithdrawalHeaderCandidates = ["withdrawal", "debit", "outflow", "paid out"];

    private readonly IUnitOfWork _unitOfWork;

    public CsvImportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CsvImportPreviewResultDto> PreviewAsync(CsvImportPreviewRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.CsvText))
            throw new ArgumentException("CSV text cannot be empty.", nameof(request.CsvText));

        var records = ParseCsv(request.CsvText);
        if (records.Count == 0)
        {
            return new CsvImportPreviewResultDto
            {
                AccountId = request.AccountId,
                MinDate = null,
                MaxDate = null,
                TotalRowCount = 0,
                ParsedRowCount = 0,
                NewCount = 0,
                DuplicateCount = 0,
                InvalidCount = 0,
                Rows = Array.Empty<CsvImportRowDto>()
            };
        }

        var startIndex = 0;
        string[]? headers = null;

        if (request.HasHeaderRow)
        {
            headers = records[0];
            startIndex = 1;
        }

        var columns = DetermineColumns(headers, records, startIndex);

        var parsed = new List<(int RowNumber, DateOnly Date, Money Amount, string Payee, string? Memo, string Fingerprint)>();
        var rows = new List<CsvImportRowDto>();

        DateOnly? minDate = null;
        DateOnly? maxDate = null;

        for (var i = startIndex; i < records.Count; i++)
        {
            var rowNumber = i + 1;
            var record = records[i];

            if (record.All(string.IsNullOrWhiteSpace))
                continue;

            if (!TryParseRow(record, columns, out var date, out var amount, out var payee, out var memo, out var error))
            {
                rows.Add(new CsvImportRowDto
                {
                    RowNumber = rowNumber,
                    Date = null,
                    Amount = null,
                    Payee = string.Empty,
                    Memo = null,
                    Status = CsvImportRowStatus.Invalid,
                    Fingerprint = null,
                    Error = error
                });

                continue;
            }

            var fingerprint = ComputeFingerprint(request.AccountId, date, amount, payee, memo);

            parsed.Add((rowNumber, date, amount, payee, memo, fingerprint));

            minDate = minDate is null || date < minDate ? date : minDate;
            maxDate = maxDate is null || date > maxDate ? date : maxDate;
        }

        var parsedCount = parsed.Count;

        var existingFingerprints = new HashSet<string>(StringComparer.Ordinal);
        if (minDate.HasValue && maxDate.HasValue && parsedCount > 0)
        {
            var existing = await _unitOfWork.Transactions.GetByAccountAndDateRangeAsync(
                request.AccountId,
                new DateRange(minDate.Value, maxDate.Value),
                ct);

            foreach (var tx in existing)
            {
                existingFingerprints.Add(ComputeFingerprint(request.AccountId, tx.Date, tx.Amount, tx.Payee, tx.Memo));
            }
        }

        var seenInFile = new HashSet<string>(StringComparer.Ordinal);
        var newCount = 0;
        var duplicateCount = 0;

        foreach (var p in parsed)
        {
            var isDuplicate = existingFingerprints.Contains(p.Fingerprint) || !seenInFile.Add(p.Fingerprint);

            var status = isDuplicate ? CsvImportRowStatus.Duplicate : CsvImportRowStatus.New;
            if (isDuplicate) duplicateCount++; else newCount++;

            rows.Add(new CsvImportRowDto
            {
                RowNumber = p.RowNumber,
                Date = p.Date,
                Amount = p.Amount,
                Payee = p.Payee,
                Memo = p.Memo,
                Status = status,
                Fingerprint = p.Fingerprint,
                Error = null
            });
        }

        rows = rows.OrderBy(r => r.RowNumber).ToList();

        return new CsvImportPreviewResultDto
        {
            AccountId = request.AccountId,
            MinDate = minDate,
            MaxDate = maxDate,
            TotalRowCount = records.Count - startIndex,
            ParsedRowCount = parsedCount,
            NewCount = newCount,
            DuplicateCount = duplicateCount,
            InvalidCount = rows.Count(r => r.Status == CsvImportRowStatus.Invalid),
            Rows = rows
        };
    }

    public async Task<HashSet<string>> GetExistingFingerprintsAsync(Guid accountId, DateRange range, CancellationToken ct = default)
    {
        var existing = await _unitOfWork.Transactions.GetByAccountAndDateRangeAsync(accountId, range, ct);
        var set = new HashSet<string>(StringComparer.Ordinal);

        foreach (var tx in existing)
        {
            set.Add(ComputeFingerprint(accountId, tx.Date, tx.Amount, tx.Payee, tx.Memo));
        }

        return set;
    }

    public static string ComputeFingerprint(Guid accountId, DateOnly date, Money amount, string payee, string? memo)
    {
        var normalizedPayee = NormalizeText(payee);
        var normalizedMemo = NormalizeText(memo);

        var key = string.Join(
            "|",
            accountId.ToString("N"),
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            amount.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            amount.Currency,
            normalizedPayee,
            normalizedMemo);

        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var trimmed = text.Trim();
        var sb = new StringBuilder(trimmed.Length);

        var lastWasWhitespace = false;
        foreach (var ch in trimmed)
        {
            var isWhitespace = char.IsWhiteSpace(ch);
            if (isWhitespace)
            {
                if (!lastWasWhitespace)
                    sb.Append(' ');
                lastWasWhitespace = true;
                continue;
            }

            lastWasWhitespace = false;
            sb.Append(ch);
        }

        return sb.ToString().ToUpperInvariant();
    }

    private sealed record CsvColumns(
        int Date,
        int Payee,
        int? Memo,
        int? Amount,
        int? Deposit,
        int? Withdrawal);

    private static CsvColumns DetermineColumns(string[]? headers, List<string[]> records, int startIndex)
    {
        if (headers is null)
        {
            // Default: Date, Payee, Amount, Memo?
            var sample = records.Skip(startIndex).FirstOrDefault(r => r.Any(s => !string.IsNullOrWhiteSpace(s))) ?? Array.Empty<string>();
            if (sample.Length < 3)
                throw new ArgumentException("CSV must have at least 3 columns when no header row is present (Date, Payee, Amount).", nameof(headers));

            return new CsvColumns(Date: 0, Payee: 1, Memo: sample.Length >= 4 ? 3 : null, Amount: 2, Deposit: null, Withdrawal: null);
        }

        var map = headers
            .Select((h, i) => (Name: (h ?? string.Empty).Trim().ToLowerInvariant(), Index: i))
            .ToDictionary(x => x.Name, x => x.Index, StringComparer.OrdinalIgnoreCase);

        int Find(string[] candidates)
        {
            foreach (var c in candidates)
            {
                if (map.TryGetValue(c, out var idx))
                    return idx;
            }

            // Fuzzy contains match (e.g., "Transaction Date")
            foreach (var (name, idx) in map)
            {
                foreach (var c in candidates)
                {
                    if (name.Contains(c, StringComparison.OrdinalIgnoreCase))
                        return idx;
                }
            }

            return -1;
        }

        var date = Find(DateHeaderCandidates);
        var payee = Find(PayeeHeaderCandidates);
        var memo = Find(MemoHeaderCandidates);

        var amount = Find(AmountHeaderCandidates);
        var deposit = Find(DepositHeaderCandidates);
        var withdrawal = Find(WithdrawalHeaderCandidates);

        if (date < 0)
            throw new ArgumentException("CSV header must include a Date column.", nameof(headers));

        if (payee < 0)
            throw new ArgumentException("CSV header must include a Payee/Description column.", nameof(headers));

        if (amount < 0 && deposit < 0 && withdrawal < 0)
            throw new ArgumentException("CSV header must include either Amount or Deposit/Withdrawal columns.", nameof(headers));

        return new CsvColumns(
            Date: date,
            Payee: payee,
            Memo: memo >= 0 ? memo : null,
            Amount: amount >= 0 ? amount : null,
            Deposit: deposit >= 0 ? deposit : null,
            Withdrawal: withdrawal >= 0 ? withdrawal : null);
    }

    private static bool TryParseRow(
        string[] record,
        CsvColumns columns,
        out DateOnly date,
        out Money amount,
        out string payee,
        out string? memo,
        out string error)
    {
        date = default;
        amount = default;
        payee = string.Empty;
        memo = null;
        error = string.Empty;

        var dateText = Get(record, columns.Date);
        if (!TryParseDate(dateText, out date))
        {
            error = $"Invalid date: '{dateText}'.";
            return false;
        }

        payee = (Get(record, columns.Payee) ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(payee))
        {
            error = "Payee/Description is required.";
            return false;
        }

        memo = columns.Memo.HasValue ? (Get(record, columns.Memo.Value) ?? string.Empty).Trim() : null;

        if (columns.Amount.HasValue)
        {
            var amountText = Get(record, columns.Amount.Value);
            if (!TryParseAmount(amountText, out amount))
            {
                error = $"Invalid amount: '{amountText}'.";
                return false;
            }
        }
        else
        {
            var depositText = columns.Deposit.HasValue ? Get(record, columns.Deposit.Value) : null;
            var withdrawalText = columns.Withdrawal.HasValue ? Get(record, columns.Withdrawal.Value) : null;

            var deposit = Money.Zero;
            var withdrawal = Money.Zero;

            if (!string.IsNullOrWhiteSpace(depositText) && !TryParseAmount(depositText, out deposit))
            {
                error = $"Invalid deposit amount: '{depositText}'.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(withdrawalText) && !TryParseAmount(withdrawalText, out withdrawal))
            {
                error = $"Invalid withdrawal amount: '{withdrawalText}'.";
                return false;
            }

            amount = deposit - withdrawal.Abs();
            if (amount.IsZero)
            {
                error = "Amount cannot be zero.";
                return false;
            }
        }

        if (amount.IsZero)
        {
            error = "Amount cannot be zero.";
            return false;
        }

        return true;
    }

    private static bool TryParseDate(string? text, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var cleaned = text.Trim();

        return DateOnly.TryParse(cleaned, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)
            || DateOnly.TryParse(cleaned, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryParseAmount(string? text, out Money money)
    {
        money = Money.Zero;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var cleaned = text.Trim();
        cleaned = cleaned.Replace("$", string.Empty, StringComparison.Ordinal);

        var negative = false;
        if (cleaned.StartsWith("(", StringComparison.Ordinal) && cleaned.EndsWith(")", StringComparison.Ordinal))
        {
            negative = true;
            cleaned = cleaned[1..^1];
        }

        if (!decimal.TryParse(
                cleaned,
                NumberStyles.Number | NumberStyles.AllowLeadingSign,
                CultureInfo.CurrentCulture,
                out var amount)
            && !decimal.TryParse(
                cleaned,
                NumberStyles.Number | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out amount))
        {
            return false;
        }

        if (negative)
            amount = -amount;

        money = new Money(amount);
        return true;
    }

    private static string? Get(string[] record, int index)
        => index >= 0 && index < record.Length ? record[index] : null;

    private static List<string[]> ParseCsv(string csvText)
    {
        var records = new List<string[]>();

        using var reader = new StringReader(csvText);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            records.Add(ParseCsvLine(line));
        }

        return records;
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();

        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                fields.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(ch);
        }

        fields.Add(sb.ToString());
        return fields.ToArray();
    }
}
