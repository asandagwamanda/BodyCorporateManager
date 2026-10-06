using BodyCorporateManager.Web.Data;
using BodyCorporateManager.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BodyCorporateManager.Web.Pages;

public class LoginModel : PageModel
{
    private readonly AppDbContext _context;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public LoginModel(AppDbContext context)
    {
        _context = context;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Message = "Please enter your username and password.";
            return Page();
        }

        var loginAccount = await _context.OwnerAccounts.FirstOrDefaultAsync(a => a.Username == Username && a.IsActive);
        if (loginAccount is null || !PasswordHelper.VerifyPassword(Password, loginAccount.PasswordHash, loginAccount.PasswordSalt))
        {
            Message = "Invalid username or password.";
            return Page();
        }

        HttpContext.Session.SetString("UnitNumber", loginAccount.UnitId.ToString());
        return RedirectToPage("/Index");
    }
}
