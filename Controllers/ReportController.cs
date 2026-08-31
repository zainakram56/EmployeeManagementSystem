using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebAppMVC.Models;
using WebAppMVC.PdfDocuments;
using QuestPDF.Fluent;

namespace WebAppMVC.Controllers
{
    [Authorize(Roles = "HR")]
    public class ReportController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ReportController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private async Task<List<EmployeeLeaveSummaryViewModel>> BuildLeaveSummaryAsync()
        {
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");

            var response = await client.GetAsync("api/LeaveBalances");
            List<LeaveBalance> balances = new();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                balances = JsonSerializer.Deserialize<List<LeaveBalance>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }

            var summary = balances
                .Where(b => b.Employee != null)
                .GroupBy(b => b.EmployeeId)
                .Select(g => new EmployeeLeaveSummaryViewModel
                {
                    EmployeeId = g.Key,
                    EmployeeName = g.First().Employee!.Name,
                    Department = g.First().Employee!.Department?.Name ?? "-",
                    LeaveTypes = g.Select(b => new LeaveTypeSummaryViewModel
                    {
                        LeaveTypeName = b.LeaveType?.Name ?? "-",
                        Allocated = b.AllocatedDays,
                        Used = b.UsedDays,
                        Remaining = b.AllocatedDays - b.UsedDays
                    }).ToList()
                })
                .OrderBy(e => e.EmployeeName)
                .ToList();

            return summary;
        }

        public async Task<IActionResult> LeaveSummary()
        {
            var summary = await BuildLeaveSummaryAsync();
            return View(summary);
        }

        public async Task<IActionResult> LeaveSummaryPdf()
        {
            var summary = await BuildLeaveSummaryAsync();

            var document = new LeaveSummaryPdfDocument(summary);
            byte[] pdfBytes = document.GeneratePdf();

            return File(pdfBytes, "application/pdf", "LeaveSummaryReport.pdf");
        }
    }
}