namespace WebAppMVC.Models
{
    public class LeaveBalance
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public int LeaveTypeId { get; set; }
        public LeaveType? LeaveType { get; set; }

        public int Year { get; set; }
        public int AllocatedDays { get; set; }
        public int UsedDays { get; set; }
    }
}