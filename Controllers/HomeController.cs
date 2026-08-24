using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebAppMVC.Data;
using WebAppMVC.Models;

namespace WebAppMVC.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(IHttpClientFactory httpClientFactory, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.EmployeeName = user?.Email;
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");

            if (user?.EmployeeId != null)
            {
                var response = await client.GetAsync($"api/Employees/{user.EmployeeId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var employee = System.Text.Json.JsonSerializer.Deserialize<Employee>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    ViewBag.ProfileName = employee?.Name;
                    ViewBag.ProfileDepartment = employee?.Department?.Name;
                }
            }

            if (User.IsInRole("Employee") || User.IsInRole("Manager"))
            {
                int currentYear = DateTime.Now.Year;

                // LeaveTypes
                var leaveTypesResponse = await client.GetAsync("api/LeaveTypes");
                List<LeaveType> leaveTypes = new();
                if (leaveTypesResponse.IsSuccessStatusCode)
                {
                    var json = await leaveTypesResponse.Content.ReadAsStringAsync();
                    leaveTypes = System.Text.Json.JsonSerializer.Deserialize<List<LeaveType>>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                // LeaveBalances (employee + year filtered)
                var balancesResponse = await client.GetAsync($"api/LeaveBalances/employee/{user!.EmployeeId}?year={currentYear}");
                List<LeaveBalance> balances = new();
                if (balancesResponse.IsSuccessStatusCode)
                {
                    var json = await balancesResponse.Content.ReadAsStringAsync();
                    balances = System.Text.Json.JsonSerializer.Deserialize<List<LeaveBalance>>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                var balanceSummary = leaveTypes.Select(lt =>
                {
                    var match = balances.FirstOrDefault(b => b.LeaveTypeId == lt.Id);
                    int allocated = match?.AllocatedDays ?? lt.DefaultDaysPerYear;
                    int used = match?.UsedDays ?? 0;
                    return new
                    {
                        lt.Id,
                        lt.Name,
                        Allocated = allocated,
                        Used = used,
                        Remaining = allocated - used
                    };
                }).ToList();

                ViewBag.BalanceSummary = balanceSummary;

                // LeaveRequests (my requests)
                var myRequestsResponse = await client.GetAsync($"api/LeaveRequests/employee/{user!.EmployeeId}");
                List<LeaveRequest> myRequests = new();
                if (myRequestsResponse.IsSuccessStatusCode)
                {
                    var json = await myRequestsResponse.Content.ReadAsStringAsync();
                    myRequests = System.Text.Json.JsonSerializer.Deserialize<List<LeaveRequest>>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                ViewBag.MyRequests = myRequests;
            }

            if (User.IsInRole("Manager"))
            {
                var allRequestsResponse = await client.GetAsync("api/LeaveRequests");
                if (allRequestsResponse.IsSuccessStatusCode)
                {
                    var json = await allRequestsResponse.Content.ReadAsStringAsync();
                    var allRequests = System.Text.Json.JsonSerializer.Deserialize<List<LeaveRequest>>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                    ViewBag.PendingTeamApprovals = allRequests.Count(lr =>
                        lr.Status == LeaveStatus.PendingManager &&
                        lr.Employee?.ManagerId == user!.EmployeeId);
                }
            }

            if (User.IsInRole("HR"))
            {
                var employeesResponse = await client.GetAsync("api/Employees");
                if (employeesResponse.IsSuccessStatusCode)
                {
                    var json = await employeesResponse.Content.ReadAsStringAsync();
                    var allEmployees = System.Text.Json.JsonSerializer.Deserialize<List<Employee>>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                    ViewBag.TotalEmployees = allEmployees.Count;
                }

                var allRequestsResponse = await client.GetAsync("api/LeaveRequests");
                if (allRequestsResponse.IsSuccessStatusCode)
                {
                    var json = await allRequestsResponse.Content.ReadAsStringAsync();
                    var allRequests = System.Text.Json.JsonSerializer.Deserialize<List<LeaveRequest>>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                    ViewBag.PendingHRApprovals = allRequests.Count(lr => lr.Status == LeaveStatus.PendingHR);
                }
            }

            return View();
        }
        public async Task<IActionResult> TestApi()
        {
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");
            var response = await client.GetAsync("api/Employees");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return Content(data, "application/json");
            }

            return Content("API call failed: " + response.StatusCode);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}