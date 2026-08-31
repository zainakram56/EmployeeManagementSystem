namespace WebAppMVC.Models
{
    public class EmployeeLeaveSummaryViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string Department { get; set; } = "";
        public List<LeaveTypeSummaryViewModel> LeaveTypes { get; set; } = new();
    }

    public class LeaveTypeSummaryViewModel
    {
        public string LeaveTypeName { get; set; } = "";
        public int Allocated { get; set; }
        public int Used { get; set; }
        public int Remaining { get; set; }
    }
}