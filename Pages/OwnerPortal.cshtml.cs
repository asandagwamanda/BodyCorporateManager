using BodyCorporateManager.Web.Data;
using BodyCorporateManager.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BodyCorporateManager.Web.Pages;

public class OwnerPortalModel : PageModel
{
    private readonly AppDbContext _context;

    public string UnitNumber { get; set; } = string.Empty;
    public Unit? Unit { get; set; }

    public OwnerPortalModel(AppDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync(string? unit)
    {
        UnitNumber = unit ?? string.Empty;
        if (string.IsNullOrWhiteSpace(UnitNumber))
        {
            return;
        }

        Unit = await _context.Units
            .Include(u => u.Payments)
            .Include(u => u.LevyStatements)
            .FirstOrDefaultAsync(u => u.UnitNumber == UnitNumber);
    }
}
