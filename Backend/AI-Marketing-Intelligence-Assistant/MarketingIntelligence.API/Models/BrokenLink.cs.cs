namespace MarketingIntelligence.API.Models
{
    public class BrokenLink
    {
        public string Url { get; set; } = string.Empty;

        public string FoundOnPage { get; set; } = string.Empty;

        public int StatusCode { get; set; }
    }
}
