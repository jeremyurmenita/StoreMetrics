using System;

namespace StoreMetrics.ViewModels
{
    public class EvaluationSummaryVm
    {
        public string? EvaluationId { get; set; }
        public string? StoreId { get; set; }
        public string StoreName { get; set; } = "";
        public double AverageRating { get; set; }
        public double PerformancePercent { get; set; }
        public string PerformanceDescription { get; set; } = "";
        public DateTime EvaluationDate { get; set; }
        public int Rank { get; set; } // ✅ For ranking display
    
        public bool IsTopPerformer { get; set; }

    }
}
