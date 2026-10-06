using BodyCorporateManager.Web.Data;
using BodyCorporateManager.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;

namespace BodyCorporateManager.Web.Pages;

public class AdministratorModel : PageModel
{
    private readonly AppDbContext _context;

    public AdministratorModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public string Message { get; set; } = string.Empty;
    public List<ImportedPaymentRow> ImportedPayments { get; set; } = new();
    public string UploadedFileName { get; set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Upload is null || Upload.Length == 0)
        {
            Message = "Please choose an Excel file to upload.";
            return Page();
        }

        if (!Upload.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            Message = "Please upload an Excel (.xlsx) file.";
            return Page();
        }

        UploadedFileName = Upload.FileName;
        ImportedPayments = new List<ImportedPaymentRow>();

        using var stream = Upload.OpenReadStream();
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
        {
            Message = "The uploaded file does not contain any worksheets.";
            return Page();
        }

        var headerMap = BuildHeaderMap(worksheet);
        var unitColumn = GetHeaderIndex(headerMap, "UnitNumber", "Unit", "Unit No", "Unit Number");
        var amountColumn = GetHeaderIndex(headerMap, "Amount", "Amount Paid", "Payment Amount");
        var paidOnColumn = GetHeaderIndex(headerMap, "PaidOn", "Date", "Payment Date", "Paid On");
        var notesColumn = GetHeaderIndex(headerMap, "Notes", "Description", "Reference");

        if (unitColumn is null || amountColumn is null)
        {
            Message = "The uploaded file must contain UnitNumber and Amount columns.";
            return Page();
        }

        var processedCount = 0;
        for (var row = 2; row <= worksheet.Dimension?.End.Row; row++)
        {
            var unitNumber = worksheet.Cells[row, unitColumn.Value].GetValue<string>()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(unitNumber))
            {
                continue;
            }

            var amountText = worksheet.Cells[row, amountColumn.Value].GetValue<string>() ?? worksheet.Cells[row, amountColumn.Value].Text;
            if (!decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) &&
                !decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.CurrentCulture, out amount))
            {
                ImportedPayments.Add(new ImportedPaymentRow
                {
                    UnitNumber = unitNumber,
                    Amount = 0,
                    PaidOn = DateTime.Now,
                    Notes = worksheet.Cells[row, notesColumn ?? 1].GetValue<string>() ?? string.Empty,
                    Status = "Invalid amount"
                });
                continue;
            }

            var paidOn = DateTime.Now;
            if (paidOnColumn is not null)
            {
                var paidOnValue = worksheet.Cells[row, paidOnColumn.Value].GetValue<DateTime?>();
                if (paidOnValue.HasValue)
                {
                    paidOn = paidOnValue.Value;
                }
            }

            var notes = worksheet.Cells[row, notesColumn ?? 1].GetValue<string>() ?? string.Empty;
            var unit = await _context.Units.FirstOrDefaultAsync(u => u.UnitNumber == unitNumber);
            if (unit is null)
            {
                ImportedPayments.Add(new ImportedPaymentRow
                {
                    UnitNumber = unitNumber,
                    Amount = amount,
                    PaidOn = paidOn,
                    Notes = notes,
                    Status = "Unit not found"
                });
                continue;
            }

            var payment = new Payment
            {
                UnitId = unit.Id,
                Amount = amount,
                PaidOn = paidOn,
                Source = "ExcelUpload",
                Notes = notes
            };

            var netOutstanding = unit.CurrentBalance + unit.DebtBalance - unit.CreditBalance;
            var remaining = netOutstanding - amount;
            unit.CurrentBalance = remaining > 0 ? remaining : 0;
            unit.DebtBalance = 0;
            unit.CreditBalance = remaining < 0 ? Math.Abs(remaining) : 0;

            _context.Payments.Add(payment);
            processedCount++;

            ImportedPayments.Add(new ImportedPaymentRow
            {
                UnitNumber = unit.UnitNumber,
                Amount = amount,
                PaidOn = paidOn,
                Notes = notes,
                Status = "Imported"
            });
        }

        await _context.SaveChangesAsync();
        Message = $"Processed {processedCount} payment update(s) from {UploadedFileName}.";
        return Page();
    }

    private static Dictionary<string, int> BuildHeaderMap(ExcelWorksheet worksheet)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = 1; col <= worksheet.Dimension?.End.Column; col++)
        {
            var value = worksheet.Cells[1, col].GetValue<string>()?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                headers[value] = col;
            }
        }

        return headers;
    }

    private static int? GetHeaderIndex(Dictionary<string, int> headerMap, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (headerMap.TryGetValue(candidate, out var index))
            {
                return index;
            }
        }

        return null;
    }
}

public class ImportedPaymentRow
{
    public string UnitNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidOn { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
