using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<LevyStatement> LevyStatements => Set<LevyStatement>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<DebtLedgerEntry> DebtLedgerEntries => Set<DebtLedgerEntry>();
    public DbSet<OwnerAccount> OwnerAccounts => Set<OwnerAccount>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=bodycorporate.db");
    }
}
