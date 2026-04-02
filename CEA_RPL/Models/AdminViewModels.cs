using System;
using System.Collections.Generic;

namespace CEA_RPL.Models
{
    public class AdminDocumentViewModel
    {
        public int ApplicantId { get; set; }
        public string ApplicantRef { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class AdminReportViewModel
    {
        public int Total { get; set; }
        public int Approved { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
        public double MonthlyGrowth { get; set; } = 8.5;
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
    }

    public class RecentActivityViewModel
    {
        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
