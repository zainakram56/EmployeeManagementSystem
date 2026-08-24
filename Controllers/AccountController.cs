using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using WebAppMVC.Data;
using WebAppMVC.Models;

namespace WebAppMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");

            var loginPayload = new { email, password };
            var content = new StringContent(
                JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/Auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                ViewBag.Error = errorMsg.Trim('"');
                return View();
            }
            var json = await response.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<LoginResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (loginResult == null || string.IsNullOrEmpty(loginResult.Token))
            {
                ViewBag.Error = "Login failed. Please try again.";
                return View();
            }

            // Token ke andar se claims nikalo
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(loginResult.Token);

            var claims = new List<Claim>(jwtToken.Claims)
            {
                new Claim("access_token", loginResult.Token) // baad mein API calls ke liye save kar rahe hain
            };

            var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var principal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);

            return RedirectToAction("Index", "Employee");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return RedirectToAction("Login", "Account");
        }

        // ---- CreateUser abhi waisa hi rahega, isko baad mein dekhenge ----

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> CreateUser()
        {
            var linkedEmployeeIds = _userManager.Users
                .Where(u => u.EmployeeId != null)
                .Select(u => u.EmployeeId)
                .ToList();

            ViewBag.AvailableEmployees = await _context.Employees
                .Include(e => e.Department)
                .Where(e => !linkedEmployeeIds.Contains(e.Id))
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> CreateUser(int employeeId, string email, string password, string role)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                EmployeeId = employeeId
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                return RedirectToAction("Index", "Employee");
            }

            ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));

            var linkedEmployeeIds = _userManager.Users
                .Where(u => u.EmployeeId != null)
                .Select(u => u.EmployeeId)
                .ToList();

            ViewBag.AvailableEmployees = await _context.Employees
                .Include(e => e.Department)
                .Where(e => !linkedEmployeeIds.Contains(e.Id))
                .ToListAsync();

            return View();
        }

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> InviteUser()
        {
            var linkedEmployeeIds = _userManager.Users
                .Where(u => u.EmployeeId != null)
                .Select(u => u.EmployeeId)
                .ToList();

            ViewBag.AvailableEmployees = await _context.Employees
                .Include(e => e.Department)
                .Where(e => !linkedEmployeeIds.Contains(e.Id))
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> InviteUser(int employeeId, string email, string role)
        {
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");

            var payload = new { employeeId, email, role };
            var content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/Auth/invite", content);

            if (response.IsSuccessStatusCode)
            {
                ViewBag.Success = "Invite sent successfully!";
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                ViewBag.Error = errorMsg;
            }

            var linkedEmployeeIds = _userManager.Users
                .Where(u => u.EmployeeId != null)
                .Select(u => u.EmployeeId)
                .ToList();

            ViewBag.AvailableEmployees = await _context.Employees
                .Include(e => e.Department)
                .Where(e => !linkedEmployeeIds.Contains(e.Id))
                .ToListAsync();

            return View();
        }

        [AllowAnonymous]
        public IActionResult Register(string token)
        {
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string token, string password, string confirmPassword)
        {
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");

            var payload = new { token, password, confirmPassword };
            var content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/Auth/register", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Account created successfully! You can now log in.";
                return RedirectToAction("Login");
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            ViewBag.Error = errorMsg;
            ViewBag.Token = token;
            return View();
        }
    }


    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int? EmployeeId { get; set; }
        public List<string>? Roles { get; set; }
    }
}