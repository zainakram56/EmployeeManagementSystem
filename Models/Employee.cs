using System.ComponentModel.DataAnnotations;

namespace WebAppMVC.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Age is required")]
        [Range(18, 65, ErrorMessage = "Age must be between 18 and 65")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Please select a department")]
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Required(ErrorMessage = "Salary is required")]
        [Range(50000, 1000000, ErrorMessage = "Salary must be between 50,000 and 1,000,000")]
        public decimal Salary { get; set; }

        public bool IsManager { get; set; } = false;

        public int? ManagerId { get; set; }
        public Employee? Manager { get; set; }
    }
}