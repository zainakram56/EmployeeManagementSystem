using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using WebAppMVC.Models;

namespace WebAppMVC.Controllers
{
    [Authorize(Roles = "Employee,Manager")]
    public class LeaveRequestController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public LeaveRequestController(
            IHttpClientFactory httpClientFactory,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
            _env = env;
        }

        private HttpClient CreateClient() => _httpClientFactory.CreateClient("WebInterfaceApi");

        private async Task<List<LeaveType>> GetLeaveTypesAsync()
        {
            var client = CreateClient();
            var response = await client.GetAsync("api/LeaveTypes");

            if (!response.IsSuccessStatusCode)
                return new List<LeaveType>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<LeaveType>>(json, _jsonOptions) ?? new();
        }

        public async Task<IActionResult> Apply()
        {
            ViewBag.LeaveTypes = await GetLeaveTypesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Apply(LeaveRequest model, IFormFile? attachmentFile)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.EmployeeId == null)
            {
                ViewBag.Error = "No employee record linked to this account.";
                ViewBag.LeaveTypes = await GetLeaveTypesAsync();
                return View(model);
            }

            if (model.EndDate < model.StartDate)
            {
                ViewBag.Error = "End date cannot be before the start date.";
                ViewBag.LeaveTypes = await GetLeaveTypesAsync();
                return View(model);
            }

            if (model.StartDate == default || model.EndDate == default)
            {
                ViewBag.Error = "Please select valid start and end dates.";
                ViewBag.LeaveTypes = await GetLeaveTypesAsync();
                return View(model);
            }

            model.EmployeeId = user.EmployeeId.Value;
            model.Status = LeaveStatus.PendingManager;
            model.AppliedOn = DateTime.Now;

            // File upload — MVC ke apne wwwroot mein hi save hoti hai
            if (attachmentFile != null && attachmentFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + attachmentFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attachmentFile.CopyToAsync(stream);
                }

                model.AttachmentPath = uniqueFileName;
            }

            // Ab API ko POST karo (sirf data, file nahi — file already save ho chuki)
            var client = CreateClient();
            var content = new StringContent(
                JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/LeaveRequests", content);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("MyRequests");
            }

            ViewBag.Error = "Failed to submit leave request: " + response.StatusCode;
            ViewBag.LeaveTypes = await GetLeaveTypesAsync();
            return View(model);
        }

        public async Task<IActionResult> MyRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            var client = CreateClient();

            var response = await client.GetAsync($"api/LeaveRequests/employee/{user!.EmployeeId}");

            List<LeaveRequest> myRequests = new();
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                myRequests = JsonSerializer.Deserialize<List<LeaveRequest>>(json, _jsonOptions) ?? new();
            }

            return View(myRequests);
        }

        public async Task<IActionResult> MyBalance()
        {
            var user = await _userManager.GetUserAsync(User);
            int currentYear = DateTime.Now.Year;
            var client = CreateClient();

            var leaveTypes = await GetLeaveTypesAsync();

            var balancesResponse = await client.GetAsync(
                $"api/LeaveBalances/employee/{user!.EmployeeId}?year={currentYear}");

            List<LeaveBalance> existingBalances = new();
            if (balancesResponse.IsSuccessStatusCode)
            {
                var json = await balancesResponse.Content.ReadAsStringAsync();
                existingBalances = JsonSerializer.Deserialize<List<LeaveBalance>>(json, _jsonOptions) ?? new();
            }

            var balanceViewList = leaveTypes.Select(lt =>
            {
                var match = existingBalances.FirstOrDefault(b => b.LeaveTypeId == lt.Id);

                int allocated = match?.AllocatedDays ?? lt.DefaultDaysPerYear;
                int used = match?.UsedDays ?? 0;

                return new LeaveBalance
                {
                    LeaveTypeId = lt.Id,
                    LeaveType = lt,
                    Year = currentYear,
                    AllocatedDays = allocated,
                    UsedDays = used
                };
            }).ToList();

            return View(balanceViewList);
        }
    }
}