using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using WebAppMVC.Models;

namespace WebAppMVC.Controllers
{
    [Authorize]
    public class ApprovalController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApprovalController(IHttpClientFactory httpClientFactory, UserManager<ApplicationUser> userManager)
        {
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
        }

        private HttpClient CreateClient() => _httpClientFactory.CreateClient("WebInterfaceApi");

        private async Task<LeaveRequest?> GetLeaveRequestAsync(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"api/LeaveRequests/{id}");

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LeaveRequest>(json, _jsonOptions);
        }

        private async Task<bool> UpdateLeaveRequestAsync(LeaveRequest request)
        {
            var client = CreateClient();
            var content = new StringContent(
                JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"api/LeaveRequests/{request.Id}", content);
            return response.IsSuccessStatusCode;
        }

        // ---------- MANAGER SIDE ----------

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ManagerQueue()
        {
            var user = await _userManager.GetUserAsync(User);
            var client = CreateClient();

            var response = await client.GetAsync("api/LeaveRequests");
            List<LeaveRequest> allRequests = new();
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                allRequests = JsonSerializer.Deserialize<List<LeaveRequest>>(json, _jsonOptions) ?? new();
            }

            var requests = allRequests
                .Where(lr => lr.Status == LeaveStatus.PendingManager
                          && lr.Employee!.ManagerId == user!.EmployeeId)
                .OrderBy(lr => lr.AppliedOn)
                .ToList();

            return View(requests);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ManagerApprove(int id, string? remarks)
        {
            var request = await GetLeaveRequestAsync(id);
            if (request == null) return NotFound();

            request.Status = LeaveStatus.PendingHR;
            request.ManagerRemarks = remarks;

            await UpdateLeaveRequestAsync(request);
            return RedirectToAction("ManagerQueue");
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ManagerReject(int id, string? remarks)
        {
            var request = await GetLeaveRequestAsync(id);
            if (request == null) return NotFound();

            request.Status = LeaveStatus.RejectedByManager;
            request.ManagerRemarks = remarks;

            await UpdateLeaveRequestAsync(request);
            return RedirectToAction("ManagerQueue");
        }

        // ---------- HR SIDE ----------

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HRQueue()
        {
            var client = CreateClient();
            var response = await client.GetAsync("api/LeaveRequests");

            List<LeaveRequest> allRequests = new();
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                allRequests = JsonSerializer.Deserialize<List<LeaveRequest>>(json, _jsonOptions) ?? new();
            }

            var requests = allRequests
                .Where(lr => lr.Status == LeaveStatus.PendingHR)
                .OrderBy(lr => lr.AppliedOn)
                .ToList();

            return View(requests);
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HRApprove(int id, string? remarks)
        {
            var client = CreateClient();

            var request = await GetLeaveRequestAsync(id);
            if (request == null) return NotFound();

            int daysRequested = (request.EndDate - request.StartDate).Days + 1;
            int year = request.StartDate.Year;

            // Existing balance dhoondo (employee + year se)
            var balancesResponse = await client.GetAsync(
                $"api/LeaveBalances/employee/{request.EmployeeId}?year={year}");

            List<LeaveBalance> balances = new();
            if (balancesResponse.IsSuccessStatusCode)
            {
                var json = await balancesResponse.Content.ReadAsStringAsync();
                balances = JsonSerializer.Deserialize<List<LeaveBalance>>(json, _jsonOptions) ?? new();
            }

            var balance = balances.FirstOrDefault(b => b.LeaveTypeId == request.LeaveTypeId);

            if (balance == null)
            {
                // LeaveType ki default days maloom karo
                var leaveTypeResponse = await client.GetAsync($"api/LeaveTypes/{request.LeaveTypeId}");
                var leaveTypeJson = await leaveTypeResponse.Content.ReadAsStringAsync();
                var leaveType = JsonSerializer.Deserialize<LeaveType>(leaveTypeJson, _jsonOptions);

                var newBalance = new LeaveBalance
                {
                    EmployeeId = request.EmployeeId,
                    LeaveTypeId = request.LeaveTypeId,
                    Year = year,
                    AllocatedDays = leaveType!.DefaultDaysPerYear,
                    UsedDays = daysRequested
                };

                var createContent = new StringContent(
                    JsonSerializer.Serialize(newBalance), Encoding.UTF8, "application/json");
                await client.PostAsync("api/LeaveBalances", createContent);
            }
            else
            {
                balance.UsedDays += daysRequested;

                var updateContent = new StringContent(
                    JsonSerializer.Serialize(balance), Encoding.UTF8, "application/json");
                await client.PutAsync($"api/LeaveBalances/{balance.Id}", updateContent);
            }

            request.Status = LeaveStatus.Approved;
            request.HRRemarks = remarks;
            await UpdateLeaveRequestAsync(request);

            return RedirectToAction("HRQueue");
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HRReject(int id, string? remarks)
        {
            var request = await GetLeaveRequestAsync(id);
            if (request == null) return NotFound();

            request.Status = LeaveStatus.RejectedByHR;
            request.HRRemarks = remarks;

            await UpdateLeaveRequestAsync(request);
            return RedirectToAction("HRQueue");
        }
    }
}