namespace MarketingIntelligence.API.Models
{
    public class SeoMetricResult
    {
        public string MetricName { get; set; } = string.Empty;

        public int Score { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;
    }
}