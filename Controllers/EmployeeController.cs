using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAppMVC.Models;
using WebAppMVC.Services;

namespace WebAppMVC.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly EmployeeService _employeeService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeController(
            EmployeeService employeeService,
            IHttpClientFactory httpClientFactory,
            UserManager<ApplicationUser> userManager)
        {
            _employeeService = employeeService;
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
        }

        private async Task<List<Department>> GetAllDepartmentsAsync()
        {
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");
            var response = await client.GetAsync("api/Departments");

            if (!response.IsSuccessStatusCode)
                return new List<Department>();

            var json = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<List<Department>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }

        private async Task PopulateDropdownsAsync(int? excludeManagerId = null)
        {
            ViewBag.Departments = await GetAllDepartmentsAsync();

            var allEmployees = await _employeeService.GetAllEmployees();
            ViewBag.Managers = allEmployees
                .Where(e => e.IsManager && e.Id != excludeManagerId)
                .ToList();
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var allEmployees = await _employeeService.GetAllEmployees();

            List<Employee> visibleEmployees;

            if (User.IsInRole("HR"))
            {
                visibleEmployees = allEmployees;
            }
            else if (User.IsInRole("Manager"))
            {
                visibleEmployees = allEmployees
                    .Where(e => e.Id == currentUser!.EmployeeId || e.ManagerId == currentUser!.EmployeeId)
                    .ToList();
            }
            else
            {
                visibleEmployees = allEmployees
                    .Where(e => e.Id == currentUser!.EmployeeId)
                    .ToList();
            }

            return View(visibleEmployees);
        }

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (!employee.IsManager && employee.ManagerId == null)
            {
                ModelState.AddModelError("ManagerId", "Manager is required unless this employee is a manager themselves.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(employee);
            }

            try
            {
                await _employeeService.AddEmployee(employee);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                await PopulateDropdownsAsync();
                return View(employee);
            }
        }

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _employeeService.GetEmployeeById(id);
            if (employee == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync(excludeManagerId: id);
            return View(employee);
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Edit(Employee employee)
        {
            if (!employee.IsManager && employee.ManagerId == null)
            {
                ModelState.AddModelError("ManagerId", "Manager is required unless this employee is a manager themselves.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(excludeManagerId: employee.Id);
                return View(employee);
            }

            try
            {
                await _employeeService.UpdateEmployee(employee);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                await PopulateDropdownsAsync(excludeManagerId: employee.Id);
                return View(employee);
            }
        }

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _employeeService.DeleteEmployee(id);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }
    }
}