using BodyCorporateManager.Web.Data;
using BodyCorporateManager.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BodyCorporateManager.Web.Pages;

public class SignupModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    [BindProperty]
    public string UnitNumber { get; set; } = string.Empty;

    [BindProperty]
    public string OwnerName { get; set; } = string.Empty;

    [BindProperty]
    public string InviteCode { get; set; } = string.Empty;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string CellphoneNumber { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public SignupModel(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(UnitNumber) || string.IsNullOrWhiteSpace(OwnerName) || string.IsNullOrWhiteSpace(InviteCode) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(CellphoneNumber) || string.IsNullOrWhiteSpace(Password))
        {
            Message = "Please fill in all required fields.";
            return Page();
        }

        var configuredInviteCode = _configuration["INVITE_CODE"] ?? Environment.GetEnvironmentVariable("INVITE_CODE") ?? string.Empty;
        if (!string.Equals(InviteCode.Trim(), configuredInviteCode.Trim(), StringComparison.Ordinal))
        {
            Message = "The invite code is invalid.";
            return Page();
        }

        if (!IsValidEmail(Username))
        {
            Message = "Username must be a valid email address.";
            return Page();
        }

        if (!IsValidPassword(Password))
        {
            Message = "Password must be at least 8 characters long and contain letters, numbers, and special characters.";
            return Page();
        }

        var unit = await _context.Units.FirstOrDefaultAsync(u => u.UnitNumber == UnitNumber);
        if (unit is null)
        {
            Message = "That unit number does not exist.";
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(OwnerName) && unit.OwnerName != OwnerName)
        {
            Message = "The owner name does not match the unit.";
            return Page();
        }

        var existing = await _context.OwnerAccounts.FirstOrDefaultAsync(a => a.Username == Username);
        if (existing is not null)
        {
            Message = "That email address already has an account.";
            return Page();
        }

        var salt = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        var hash = PasswordHelper.HashPassword(Password, salt);

        _context.OwnerAccounts.Add(new OwnerAccount
        {
            UnitId = unit.Id,
            Username = Username,
            CellphoneNumber = CellphoneNumber,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        Message = "Account created successfully. You can now sign in.";
        return Page();
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            var email = new System.Net.Mail.MailAddress(value);
            return email.Address == value;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPassword(string password)
    {
        if (password.Length < 8)
        {
            return false;
        }

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasLetter && hasDigit && hasSpecial;
    }
}
