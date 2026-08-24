namespace WebAppMVC.Models
{
    public class LeaveType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g. "Casual", "Sick", "Annual"
        public int DefaultDaysPerYear { get; set; }
    }
}