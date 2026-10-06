using BodyCorporateManager.Web.Data;
using BodyCorporateManager.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BodyCorporateManager.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public List<Unit> Units { get; set; } = new();
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
    public List<Payment> RecentPayments { get; set; } = new();

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        var unitNumberText = HttpContext.Session.GetString("UnitNumber");
        if (string.IsNullOrEmpty(unitNumberText))
        {
            Response.Redirect("/Login");
            return;
        }

        if (!int.TryParse(unitNumberText, out var unitId))
        {
            Response.Redirect("/Login");
            return;
        }

        var unit = await _context.Units
            .Include(u => u.Payments)
            .Include(u => u.LevyStatements)
            .FirstOrDefaultAsync(u => u.Id == unitId);

        if (unit is null)
        {
            Response.Redirect("/Login");
            return;
        }

        Units = new List<Unit> { unit };
        TotalPaid = unit.Payments.Sum(p => p.Amount);
        OutstandingBalance = Math.Max(0, unit.CurrentBalance + unit.DebtBalance - unit.CreditBalance);
        RecentPayments = unit.Payments.OrderByDescending(p => p.PaidOn).Take(5).ToList();
    }
}
