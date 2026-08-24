using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebAppMVC.Data;
using WebAppMVC.Models;

namespace WebAppMVC.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var context = services.GetRequiredService<ApplicationDbContext>();
            

            // 1. Create roles if they don't exist
            string[] roles = { "Employee", "Manager", "HR" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
            // 1.5. Seed leave types if they don't exist
            if (!context.LeaveTypes.Any())
            {
                context.LeaveTypes.AddRange(
                    new LeaveType { Name = "Casual", DefaultDaysPerYear = 10 },
                    new LeaveType { Name = "Sick", DefaultDaysPerYear = 8 },
                    new LeaveType { Name = "Annual", DefaultDaysPerYear = 14 }
                );
                await context.SaveChangesAsync();
            }
            // 1.6. Seed departments if they don't exist
            if (!context.Departments.Any())
            {
                context.Departments.AddRange(
                    new Department { Name = "Development" },
                    new Department { Name = "Human Resources" },
                    new Department { Name = "Marketing" },
                    new Department { Name = "Accounts" },
                    new Department { Name = "Sales" }
                );
                await context.SaveChangesAsync();
            }

            // 2. Only seed users if none exist yet
            if (await userManager.Users.AnyAsync())
            {
                return;
            }

            // 3. Create Employee records first
            var hrDept = context.Departments.First(d => d.Name == "Human Resources");
            var devDept = context.Departments.First(d => d.Name == "Development");

            var hrEmployee = new Employee
            {
                Name = "Sara HR",
                Age = 30,
                DepartmentId = hrDept.Id,
                Salary = 60000,
                IsManager = false
            };

            var managerEmployee = new Employee
            {
                Name = "Ali Manager",
                Age = 35,
                DepartmentId = devDept.Id,
                Salary = 90000,
                IsManager = true
            };

            context.Employees.AddRange(hrEmployee, managerEmployee);
            await context.SaveChangesAsync();

            var staffEmployee = new Employee
            {
                Name = "Zain Employee",
                Age = 24,
                DepartmentId = devDept.Id,
                Salary = 50000,
                IsManager = false,
                ManagerId = managerEmployee.Id
            };

            context.Employees.Add(staffEmployee);
            await context.SaveChangesAsync();

            // 4. Create login users linked to those Employee records
            await CreateUserAsync(userManager, "hr@company.com", "Hr@12345", "HR", hrEmployee.Id);
            await CreateUserAsync(userManager, "manager@company.com", "Manager@12345", "Manager", managerEmployee.Id);
            await CreateUserAsync(userManager, "employee@company.com", "Employee@12345", "Employee", staffEmployee.Id);
        }

        private static async Task CreateUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string role,
            int employeeId)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                EmployeeId = employeeId
            };

            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}