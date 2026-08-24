using Microsoft.AspNetCore.Identity;

namespace WebAppMVC.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }
    }
}
