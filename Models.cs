using System.ComponentModel.DataAnnotations;

public class Unit
{
    public int Id { get; set; }

    [Required]
    public string UnitNumber { get; set; } = string.Empty;

    [Required]
    public string OwnerName { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal SquareMeters { get; set; }

    [Range(0, 100000)]
    public decimal LevyRatePerSquareMeter { get; set; }

    public decimal CurrentBalance { get; set; }

    public decimal DebtBalance { get; set; }

    public decimal CreditBalance { get; set; }

    public List<LevyStatement> LevyStatements { get; set; } = new();

    public List<Payment> Payments { get; set; } = new();

    public List<DebtLedgerEntry> DebtEntries { get; set; } = new();

    public List<OwnerAccount> OwnerAccounts { get; set; } = new();
}

public class LevyStatement
{
    public int Id { get; set; }

    public int UnitId { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal OutstandingAmount { get; set; }

    public bool IsClosed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Unit? Unit { get; set; }
}

public class Payment
{
    public int Id { get; set; }

    public int UnitId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaidOn { get; set; } = DateTime.UtcNow;

    public string Source { get; set; } = "Manual";

    public string Notes { get; set; } = string.Empty;

    public Unit? Unit { get; set; }
}

public class DebtLedgerEntry
{
    public int Id { get; set; }

    public int UnitId { get; set; }

    public decimal Amount { get; set; }

    public string EntryType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime RecordedOn { get; set; } = DateTime.UtcNow;

    public Unit? Unit { get; set; }
}

public class OwnerAccount
{
    public int Id { get; set; }

    public int UnitId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Unit? Unit { get; set; }
}
