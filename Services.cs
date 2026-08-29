using Microsoft.EntityFrameworkCore;

public static class LevyService
{
    public static decimal CalculateLevyAmount(Unit unit, DateTime periodStart, DateTime periodEnd)
    {
        if (unit.SquareMeters <= 0 || unit.LevyRatePerSquareMeter <= 0)
            return 0;

        return unit.SquareMeters * unit.LevyRatePerSquareMeter;
    }

    public static void ApplyPayment(AppDbContext context, Unit unit, decimal amount, string source, string notes, DateTime? paidOn = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive.");

        var payment = new Payment
        {
            UnitId = unit.Id,
            Amount = amount,
            Source = source,
            Notes = notes,
            PaidOn = paidOn ?? DateTime.UtcNow
        };

        unit.Payments.Add(payment);

        decimal remaining = amount;

        if (unit.CurrentBalance > 0)
        {
            var toCurrent = Math.Min(remaining, unit.CurrentBalance);
            unit.CurrentBalance -= toCurrent;
            remaining -= toCurrent;
        }

        if (remaining > 0)
        {
            var debtEntries = context.DebtLedgerEntries
                .Where(entry => entry.UnitId == unit.Id && entry.Amount > 0)
                .OrderBy(entry => entry.RecordedOn)
                .ThenBy(entry => entry.Id)
                .ToList();

            foreach (var debtEntry in debtEntries)
            {
                if (remaining <= 0)
                    break;

                var applied = Math.Min(remaining, debtEntry.Amount);
                debtEntry.Amount -= applied;
                remaining -= applied;
            }
        }

        if (remaining > 0)
        {
            unit.CreditBalance += remaining;
        }

        unit.DebtBalance = context.DebtLedgerEntries.Where(entry => entry.UnitId == unit.Id).Sum(entry => entry.Amount);
        unit.CurrentBalance = Math.Max(0, unit.CurrentBalance);
        unit.DebtBalance = Math.Max(0, unit.DebtBalance);
        unit.CreditBalance = Math.Max(0, unit.CreditBalance);
    }
}
