
using Microsoft.AspNetCore.Mvc;
using MarketingIntelligence.API.Models;
using MarketingIntelligence.API.Services;

namespace MarketingIntelligence.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly WebsiteCrawlerService _crawlerService;
        private readonly GeminiService _geminiService;

        public AuditController(
            WebsiteCrawlerService crawlerService,
            GeminiService geminiService)
        {
            _crawlerService = crawlerService;
            _geminiService = geminiService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAudit(
            [FromBody] AuditRequest request)
        {
            try
            {
                var crawledPages =
                    await _crawlerService.CrawlWebsiteAsync(request.Url);

                if (crawledPages.Count == 0)
                {
                    return BadRequest(new
                    {
                        message = "Unable to crawl the website.",
                        reason = "The website denied access or could not be reached."
                    });
                }

                var pageReports = new List<object>();
                int totalScore = 0;

                foreach (var page in crawledPages)
                {
                    string html = page.HtmlContent;

                    string? title =
                        _crawlerService.GetPageTitle(html);

                    string? metaDescription =
                        _crawlerService.GetMetaDescription(html);

                    int h1Count =
                        _crawlerService.GetH1Count(html);

                    string? canonicalUrl =
                        _crawlerService.GetCanonicalUrl(html);

                    int jsonLdCount =
                        _crawlerService.GetJsonLdCount(html);

                    var brokenLinks =
                        await _crawlerService.GetBrokenInternalLinksAsync(
                            html, page.Url);

                    var metrics = new List<SeoMetricResult>
                    {
                        new SeoMetricResult
                        {
                            MetricName = "Title and Meta Description",
                            Score =
                                (title?.Length >= 30 && title.Length <= 60 ? 15 : 0)
                                +
                                (metaDescription?.Length >= 70 &&
                                 metaDescription.Length <= 160 ? 15 : 0),
                            Status =
                                (title?.Length >= 30 && title.Length <= 60 &&
                                 metaDescription?.Length >= 70 &&
                                 metaDescription.Length <= 160)
                                ? "Pass" : "Fail",
                            Details =
                                $"Title: {title?.Length ?? 0} characters, " +
                                $"Meta description: {metaDescription?.Length ?? 0} characters"
                        },
                        new SeoMetricResult
                        {
                            MetricName = "Broken Internal Links",
                            Score = brokenLinks.Count == 0 ? 25 : 0,
                            Status = brokenLinks.Count == 0 ? "Pass" : "Fail",
                            Details = $"{brokenLinks.Count} broken links found"
                        },
                        new SeoMetricResult
                        {
                            MetricName = "H1 Heading",
                            Score = h1Count == 1 ? 15 : 0,
                            Status = h1Count == 1 ? "Pass" : "Fail",
                            Details = $"{h1Count} H1 headings found"
                        },
                        new SeoMetricResult
                        {
                            MetricName = "Canonical URL",
                            Score = canonicalUrl != null ? 15 : 0,
                            Status = canonicalUrl != null ? "Pass" : "Fail",
                            Details = canonicalUrl ?? "Canonical URL missing"
                        },
                        new SeoMetricResult
                        {
                            MetricName = "Structured Data (JSON-LD)",
                            Score = jsonLdCount > 0 ? 15 : 0,
                            Status = jsonLdCount > 0 ? "Pass" : "Fail",
                            Details = $"{jsonLdCount} JSON-LD blocks found"
                        }
                    };

                    int pageScore = metrics.Sum(m => m.Score);
                    totalScore += pageScore;

                    pageReports.Add(new
                    {
                        url = page.Url,
                        pageTitle = title,
                        titleLength = title?.Length ?? 0,
                        metaDescription,
                        metaDescriptionLength = metaDescription?.Length ?? 0,
                        h1Count,
                        canonicalUrl,
                        jsonLdCount,
                        brokenLinks,
                        brokenLinksCount = brokenLinks.Count,
                        seoMetrics = metrics,
                        seoScore = pageScore
                    });
                }

                int overallScore = crawledPages.Count > 0
                    ? totalScore / crawledPages.Count
                    : 0;

                string prompt = $@"
You are an SEO expert.

Analyze these website SEO audit findings:

Website: {request.Url}
Overall SEO Score: {overallScore}/100

Audit findings:
{System.Text.Json.JsonSerializer.Serialize(pageReports)}

Provide:
1. The top SEO problems.
2. A prioritized action list.
3. Short, practical recommendations.

Use only the supplied audit findings.
Do not calculate or change the SEO score.
Do not invent missing information.
";

                string recommendations =
                    await _geminiService.GenerateRecommendationsAsync(prompt);

                return Ok(new
                {
                    websiteUrl = request.Url,
                    totalPagesCrawled = crawledPages.Count,
                    overallSeoScore = overallScore,
                    pages = pageReports,
                    recommendations
                });
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(new
                {
                    message = "Unable to access the website.",
                    reason = ex.Message,
                    statusCode = ex.StatusCode.HasValue
                        ? (int)ex.StatusCode.Value
                        : 0
                });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new
                {
                    message = "The website request timed out. Please try again."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while generating the SEO audit.",
                    error = ex.Message
                });
            }
        }
    }
}
