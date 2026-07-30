using System.ComponentModel.DataAnnotations;

namespace StoreMetrics.ViewModels
{
    public class StoreReportVm
    {
        [Required] public string StoreId { get; set; } = "";
        public string StoreName { get; set; } = "";

        // Averages for the selected store
        public double Cleanliness { get; set; }
        public double Condition { get; set; }
        public double CustomerEngagement { get; set; }
        public double PersonalGrooming { get; set; }
        public double Accuracy { get; set; }
        public double SpeedOfService { get; set; }
        public double ProductQuality { get; set; }

        public bool HasData { get; set; } // true if the store has at least one evaluation
    }
}
