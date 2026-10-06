using BodyCorporateManager.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BodyCorporateManager.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<LevyStatement> LevyStatements => Set<LevyStatement>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<DebtLedgerEntry> DebtLedgerEntries => Set<DebtLedgerEntry>();
    public DbSet<OwnerAccount> OwnerAccounts => Set<OwnerAccount>();
}
