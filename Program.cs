using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BodyCorporateManager.Web;
using BodyCorporateManager.Web.Data;
using BodyCorporateManager.Web.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ??
    Path.Combine(AppContext.BaseDirectory, "bodycorporate.db");
var jwtKey = builder.Configuration["JWT_KEY"] ?? Environment.GetEnvironmentVariable("JWT_KEY") ?? "change-this-in-prod";
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "BodyCorp";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "BodyCorpClients";
var inviteCode = builder.Configuration["INVITE_CODE"] ?? Environment.GetEnvironmentVariable("INVITE_CODE") ?? string.Empty;
var adminToken = builder.Configuration["ADMIN_TOKEN"] ?? Environment.GetEnvironmentVariable("ADMIN_TOKEN") ?? string.Empty;

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
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

    var seedFlag = builder.Configuration["SEED_UNIT_ON_STARTUP"] ?? Environment.GetEnvironmentVariable("SEED_UNIT_ON_STARTUP");
    if (!string.IsNullOrEmpty(seedFlag) && seedFlag == "1" && !db.Units.Any(u => u.UnitNumber == "UNIT-2"))
    {
        db.Units.Add(new Unit
        {
            UnitNumber = "UNIT-2",
            OwnerName = "Asanda",
            SquareMeters = 50,
            LevyRatePerSquareMeter = 10,
            CurrentBalance = 0,
            DebtBalance = 0,
            CreditBalance = 0
        });
        db.SaveChanges();
    }
}

app.MapGet("/", () => Results.Ok(new { message = "Hello — BodyCorporateManager API is running." }));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/register", async (RegisterRequest req, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(inviteCode) || req.InviteCode != inviteCode)
        return Results.Forbid();

    var unit = db.Units.FirstOrDefault(u => u.UnitNumber == req.UnitNumber);
    if (unit is null)
        return Results.BadRequest(new { error = "Unit not found" });

    if (db.OwnerAccounts.Any(a => a.Username == req.Username))
        return Results.BadRequest(new { error = "Username already exists" });

    var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    var hash = PasswordHelper.HashPassword(req.Password, salt);

    var account = new OwnerAccount
    {
        UnitId = unit.Id,
        Username = req.Username,
        PasswordHash = hash,
        PasswordSalt = salt,
        IsActive = true
    };

    db.OwnerAccounts.Add(account);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Registered" });
});

app.MapPost("/login", async (LoginRequest req, AppDbContext db) =>
{
    var account = db.OwnerAccounts.FirstOrDefault(a => a.Username == req.Username && a.IsActive);
    if (account is null || !PasswordHelper.VerifyPassword(req.Password, account.PasswordHash, account.PasswordSalt))
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
        new Claim(ClaimTypes.Name, account.Username),
        new Claim("unitId", account.UnitId.ToString())
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds);

    var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = tokenString });
});

app.MapGet("/me", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(idClaim, out var id))
        return Results.Unauthorized();

    var account = db.OwnerAccounts.Include(a => a.Unit).FirstOrDefault(a => a.Id == id);
    if (account is null)
        return Results.NotFound();

    return Results.Ok(new
    {
        account.Username,
        account.UnitId,
        Unit = new
        {
            account.Unit?.UnitNumber,
            account.Unit?.OwnerName,
            account.Unit?.CurrentBalance,
            account.Unit?.DebtBalance,
            account.Unit?.CreditBalance
        }
    });
}).RequireAuthorization();

app.MapPost("/admin/create-unit", async (CreateUnitRequest req, HttpRequest http, AppDbContext db) =>
{
    var provided = http.Headers["X-Admin-Token"].FirstOrDefault();
    if (string.IsNullOrEmpty(adminToken) || string.IsNullOrEmpty(provided) || provided != adminToken)
        return Results.Unauthorized();

    var exists = db.Units.Any(u => u.UnitNumber == req.UnitNumber);
    if (exists)
        return Results.BadRequest(new { error = "Unit already exists" });

    var unit = new Unit
    {
        UnitNumber = req.UnitNumber,
        OwnerName = req.OwnerName ?? string.Empty,
        SquareMeters = req.SquareMeters,
        LevyRatePerSquareMeter = req.LevyRate,
        CurrentBalance = 0,
        DebtBalance = 0,
        CreditBalance = 0
    };

    db.Units.Add(unit);
    await db.SaveChangesAsync();
    return Results.Ok(new { created = true, unitId = unit.Id });
});

app.MapRazorPages();
app.Run();

public record RegisterRequest(string UnitNumber, string Username, string Password, string InviteCode);
public record LoginRequest(string Username, string Password);
public record CreateUnitRequest(string UnitNumber, string OwnerName, decimal SquareMeters, decimal LevyRate);
