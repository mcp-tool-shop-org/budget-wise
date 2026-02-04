namespace BudgetWise.Infrastructure.Database;

/// <summary>
/// SQL schema definitions for SQLite database.
/// </summary>
public static class DatabaseSchema
{
    public const string CreateAccountsTable = """
        CREATE TABLE IF NOT EXISTS Accounts (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            Type INTEGER NOT NULL,
            Balance REAL NOT NULL DEFAULT 0,
            ClearedBalance REAL NOT NULL DEFAULT 0,
            UnclearedBalance REAL NOT NULL DEFAULT 0,
            Currency TEXT NOT NULL DEFAULT 'USD',
            IsActive INTEGER NOT NULL DEFAULT 1,
            IsOnBudget INTEGER NOT NULL DEFAULT 1,
            SortOrder INTEGER NOT NULL DEFAULT 0,
            Note TEXT,
            LastReconciledAt TEXT,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_Accounts_IsActive ON Accounts(IsActive);
        CREATE INDEX IF NOT EXISTS IX_Accounts_Type ON Accounts(Type);
        """;

    public const string CreateEnvelopesTable = """
        CREATE TABLE IF NOT EXISTS Envelopes (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            GroupName TEXT,
            Color TEXT NOT NULL DEFAULT '#5B9BD5',
            SortOrder INTEGER NOT NULL DEFAULT 0,
            IsActive INTEGER NOT NULL DEFAULT 1,
            IsHidden INTEGER NOT NULL DEFAULT 0,
            GoalAmount REAL,
            GoalCurrency TEXT,
            GoalDate TEXT,
            Note TEXT,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_Envelopes_IsActive ON Envelopes(IsActive);
        CREATE INDEX IF NOT EXISTS IX_Envelopes_GroupName ON Envelopes(GroupName);
        """;

    public const string CreateTransactionsTable = """
        CREATE TABLE IF NOT EXISTS Transactions (
            Id TEXT PRIMARY KEY,
            AccountId TEXT NOT NULL,
            EnvelopeId TEXT,
            TransferAccountId TEXT,
            LinkedTransactionId TEXT,
            Date TEXT NOT NULL,
            Amount REAL NOT NULL,
            Currency TEXT NOT NULL DEFAULT 'USD',
            Payee TEXT NOT NULL,
            Memo TEXT,
            Type INTEGER NOT NULL,
            IsCleared INTEGER NOT NULL DEFAULT 0,
            IsReconciled INTEGER NOT NULL DEFAULT 0,
            IsApproved INTEGER NOT NULL DEFAULT 1,
            IsDeleted INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            FOREIGN KEY (AccountId) REFERENCES Accounts(Id),
            FOREIGN KEY (EnvelopeId) REFERENCES Envelopes(Id),
            FOREIGN KEY (TransferAccountId) REFERENCES Accounts(Id),
            FOREIGN KEY (LinkedTransactionId) REFERENCES Transactions(Id)
        );
        CREATE INDEX IF NOT EXISTS IX_Transactions_AccountId ON Transactions(AccountId);
        CREATE INDEX IF NOT EXISTS IX_Transactions_AccountId_Date ON Transactions(AccountId, Date);
        CREATE INDEX IF NOT EXISTS IX_Transactions_EnvelopeId ON Transactions(EnvelopeId);
        CREATE INDEX IF NOT EXISTS IX_Transactions_Date ON Transactions(Date);
        CREATE INDEX IF NOT EXISTS IX_Transactions_IsCleared ON Transactions(IsCleared);
        CREATE INDEX IF NOT EXISTS IX_Transactions_IsReconciled ON Transactions(IsReconciled);
        CREATE INDEX IF NOT EXISTS IX_Transactions_IsDeleted ON Transactions(IsDeleted);
        """;

    public const string CreateBudgetPeriodsTable = """
        CREATE TABLE IF NOT EXISTS BudgetPeriods (
            Id TEXT PRIMARY KEY,
            Year INTEGER NOT NULL,
            Month INTEGER NOT NULL,
            TotalIncome REAL NOT NULL DEFAULT 0,
            TotalAllocated REAL NOT NULL DEFAULT 0,
            TotalSpent REAL NOT NULL DEFAULT 0,
            CarriedOver REAL NOT NULL DEFAULT 0,
            Currency TEXT NOT NULL DEFAULT 'USD',
            IsClosed INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            UNIQUE(Year, Month)
        );
        CREATE INDEX IF NOT EXISTS IX_BudgetPeriods_YearMonth ON BudgetPeriods(Year, Month);
        """;

    public const string CreateEnvelopeAllocationsTable = """
        CREATE TABLE IF NOT EXISTS EnvelopeAllocations (
            Id TEXT PRIMARY KEY,
            EnvelopeId TEXT NOT NULL,
            BudgetPeriodId TEXT NOT NULL,
            Allocated REAL NOT NULL DEFAULT 0,
            RolloverFromPrevious REAL NOT NULL DEFAULT 0,
            Spent REAL NOT NULL DEFAULT 0,
            Currency TEXT NOT NULL DEFAULT 'USD',
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            FOREIGN KEY (EnvelopeId) REFERENCES Envelopes(Id),
            FOREIGN KEY (BudgetPeriodId) REFERENCES BudgetPeriods(Id),
            UNIQUE(EnvelopeId, BudgetPeriodId)
        );
        CREATE INDEX IF NOT EXISTS IX_EnvelopeAllocations_EnvelopeId ON EnvelopeAllocations(EnvelopeId);
        CREATE INDEX IF NOT EXISTS IX_EnvelopeAllocations_BudgetPeriodId ON EnvelopeAllocations(BudgetPeriodId);
        """;

    public const string CreatePayeesTable = """
        CREATE TABLE IF NOT EXISTS Payees (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL UNIQUE,
            DefaultEnvelopeId TEXT,
            IsHidden INTEGER NOT NULL DEFAULT 0,
            TransactionCount INTEGER NOT NULL DEFAULT 0,
            LastUsedAt TEXT,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            FOREIGN KEY (DefaultEnvelopeId) REFERENCES Envelopes(Id)
        );
        CREATE INDEX IF NOT EXISTS IX_Payees_Name ON Payees(Name);
        CREATE INDEX IF NOT EXISTS IX_Payees_IsHidden ON Payees(IsHidden);
        """;

    public const string CreateTransactionSplitsTable = """
        CREATE TABLE IF NOT EXISTS TransactionSplits (
            Id TEXT PRIMARY KEY,
            TransactionId TEXT NOT NULL,
            EnvelopeId TEXT NOT NULL,
            Amount REAL NOT NULL,
            Currency TEXT NOT NULL DEFAULT 'USD',
            SortOrder INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            FOREIGN KEY (TransactionId) REFERENCES Transactions(Id),
            FOREIGN KEY (EnvelopeId) REFERENCES Envelopes(Id)
        );
        CREATE INDEX IF NOT EXISTS IX_TransactionSplits_TransactionId ON TransactionSplits(TransactionId);
        CREATE INDEX IF NOT EXISTS IX_TransactionSplits_EnvelopeId ON TransactionSplits(EnvelopeId);
        CREATE INDEX IF NOT EXISTS IX_TransactionSplits_EnvelopeId_TransactionId ON TransactionSplits(EnvelopeId, TransactionId);
        """;

    public static readonly string[] AllTables =
    [
        CreateAccountsTable,
        CreateEnvelopesTable,
        CreateTransactionsTable,
        CreateBudgetPeriodsTable,
        CreateEnvelopeAllocationsTable,
        CreatePayeesTable,
        CreateTransactionSplitsTable
    ];
}
