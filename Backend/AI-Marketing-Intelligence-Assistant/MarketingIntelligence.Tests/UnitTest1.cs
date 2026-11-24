
using MarketingIntelligence.API.Services;
using MarketingIntelligence.API.Models;
using System.ComponentModel.DataAnnotations;

namespace MarketingIntelligence.Tests
{
    public class UnitTest1
    {
        private readonly WebsiteCrawlerService _crawlerService;

        public UnitTest1()
        {
            _crawlerService = new WebsiteCrawlerService(
                new HttpClient());
        }

        [Fact]
        public void GetPageTitle_ReturnsCorrectTitle()
        {
            string html = "<html><head><title>Test Website</title></head></html>";

            var result = _crawlerService.GetPageTitle(html);

            Assert.Equal("Test Website", result);
        }

        [Fact]
        public void GetMetaDescription_ReturnsCorrectDescription()
        {
            string html = "<html><head><meta name='description' content='Test description'></head></html>";

            var result = _crawlerService.GetMetaDescription(html);

            Assert.Equal("Test description", result);
        }

        [Fact]
        public void GetH1Count_ReturnsCorrectCount()
        {
            string html = "<html><body><h1>Heading 1</h1><h1>Heading 2</h1></body></html>";

            var result = _crawlerService.GetH1Count(html);

            Assert.Equal(2, result);
        }

        [Fact]
        public void GetCanonicalUrl_ReturnsValidUrl()
        {
            string html = "<html><head><link rel='canonical' href='https://example.com/'></head></html>";

            var result = _crawlerService.GetCanonicalUrl(html);

            Assert.Equal("https://example.com/", result);
        }

        [Fact]
        public void GetJsonLdCount_ReturnsCorrectCount()
        {
            string html = "<html><body><script type='application/ld+json'>{}</script></body></html>";

            var result = _crawlerService.GetJsonLdCount(html);

            Assert.Equal(1, result);
        }

        // Edge case: Missing page title
        [Fact]
        public void GetPageTitle_ReturnsNull_WhenTitleIsMissing()
        {
            string html = "<html><head></head><body></body></html>";

            var result = _crawlerService.GetPageTitle(html);

            Assert.Null(result);
        }

        // Edge case: No H1 headings
        [Fact]
        public void GetH1Count_ReturnsZero_WhenNoH1Exists()
        {
            string html = "<html><body><h2>Heading</h2></body></html>";

            var result = _crawlerService.GetH1Count(html);

            Assert.Equal(0, result);
        }

        // Invalid input: FTP URL should not be accepted
        [Fact]
        public void AuditRequest_IsInvalid_WhenFtpUrlIsProvided()
        {
            var request = new AuditRequest
            {
                Url = "ftp://example.com"
            };

            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                validationResults,
                validateAllProperties: true);

            Assert.False(isValid);
        }
    }
}
