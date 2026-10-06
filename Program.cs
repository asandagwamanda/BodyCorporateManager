using BodyCorporateManager.Web.Data;
using BodyCorporateManager.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ??
    Path.Combine(AppContext.BaseDirectory, "bodycorporate.db");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@bodycorporate.local";
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@123";
    var adminUnitNumber = Environment.GetEnvironmentVariable("ADMIN_UNIT_NUMBER") ?? "ADMIN-001";
    var adminOwnerName = Environment.GetEnvironmentVariable("ADMIN_OWNER_NAME") ?? "System Administrator";

    var unit = db.Units.FirstOrDefault(u => u.UnitNumber == adminUnitNumber);
    if (unit is null)
    {
        unit = new Unit
        {
            UnitNumber = adminUnitNumber,
            OwnerName = adminOwnerName,
            SquareMeters = 0,
            LevyRatePerSquareMeter = 0,
            CurrentBalance = 0,
            DebtBalance = 0,
            CreditBalance = 0
        };

        db.Units.Add(unit);
        db.SaveChanges();
    }

    var hasAdmin = db.OwnerAccounts.Any(a => a.Username == adminEmail);
    if (!hasAdmin)
    {
        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var hash = PasswordHelper.HashPassword(adminPassword, salt);

        db.OwnerAccounts.Add(new OwnerAccount
        {
            UnitId = unit.Id,
            Username = adminEmail,
            CellphoneNumber = "+0000000000",
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        db.SaveChanges();
    }
}

app.MapRazorPages();

app.Run();
