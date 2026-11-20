namespace Backend.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalRoles { get; set; }
        public int TotalMenus { get; set; }
        public int TotalMenuItems { get; set; }
        public int ActiveUsers { get; set; }
        public int ActiveRoles { get; set; }
        public int ActiveMenus { get; set; }
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public int ActiveMockProjects { get; set; }
        public int TotalAllocations { get; set; }
        public double AverageAttendanceRate { get; set; } = 100.0;
        public double OnTrackRate { get; set; } = 100.0;
        public Dictionary<long, double> ClassAttendanceRates { get; set; } = new();
    }
}

