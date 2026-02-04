using NextLedger.Domain.Common;

namespace NextLedger.Domain.Entities;

/// <summary>
/// Represents a payee (merchant, vendor, employer, etc.)
/// Payees can have a default envelope for auto-categorization.
/// </summary>
public class Payee : Entity
{
    public string Name { get; private set; }
    public Guid? DefaultEnvelopeId { get; private set; }
    public bool IsHidden { get; private set; }
    public int TransactionCount { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private Payee() : base()
    {
        Name = string.Empty;
    }

    public static Payee Create(string name, Guid? defaultEnvelopeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Payee name cannot be empty.", nameof(name));

        return new Payee
        {
            Name = NormalizeName(name),
            DefaultEnvelopeId = defaultEnvelopeId,
            IsHidden = false,
            TransactionCount = 0
        };
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Payee name cannot be empty.", nameof(newName));

        Name = NormalizeName(newName);
        Touch();
    }

    public void SetDefaultEnvelope(Guid? envelopeId)
    {
        DefaultEnvelopeId = envelopeId;
        Touch();
    }

    public void Hide()
    {
        IsHidden = true;
        Touch();
    }

    public void Show()
    {
        IsHidden = false;
        Touch();
    }

    public void RecordUsage()
    {
        TransactionCount++;
        LastUsedAt = DateTime.UtcNow;
        Touch();
    }

    private static string NormalizeName(string name)
    {
        // Normalize whitespace and trim
        return string.Join(" ", name.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));
    }
}
