namespace WebAppMVC.Models
{
    public enum LeaveStatus
    {
        PendingManager,
        PendingHR,
        Approved,
        RejectedByManager,
        RejectedByHR
    }

    public class LeaveRequest
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public int LeaveTypeId { get; set; }
        public LeaveType? LeaveType { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;

        public LeaveStatus Status { get; set; } = LeaveStatus.PendingManager;

        public string? ManagerRemarks { get; set; }
        public string? HRRemarks { get; set; }
        public string? AttachmentPath { get; set; }

        public DateTime AppliedOn { get; set; } = DateTime.Now;
    }
}