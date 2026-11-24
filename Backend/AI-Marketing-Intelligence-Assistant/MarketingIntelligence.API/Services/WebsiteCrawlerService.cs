
using HtmlAgilityPack;
using MarketingIntelligence.API.Models;
using System.Net;
using System.Net.Sockets;

namespace MarketingIntelligence.API.Services
{
    public class WebsiteCrawlerService
    {
        private readonly HttpClient _httpClient;

        private readonly List<(string Path, bool Allow)> _robotsRules = new();
        private bool _robotsLoaded;
        private bool _blockAllCrawling;

        public WebsiteCrawlerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(10);

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36");
        }

        // Validate URL and block private/local IP addresses
        private async Task ValidatePublicUrlAsync(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                throw new HttpRequestException("Invalid website URL.");

            if (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new HttpRequestException(
                    "Only HTTP and HTTPS URLs are allowed.");
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
                throw new HttpRequestException(
                    "URLs containing username or password are not allowed.");

            IPAddress[] addresses;

            try
            {
                addresses = await Dns.GetHostAddressesAsync(uri.Host);
            }
            catch
            {
                throw new HttpRequestException(
                    "Unable to resolve website address.");
            }

            if (addresses.Length == 0)
                throw new HttpRequestException(
                    "Website address could not be found.");

            foreach (var address in addresses)
            {
                if (IsPrivateOrLocalIp(address))
                {
                    throw new HttpRequestException(
                        "Access to localhost and private IP addresses is not allowed.");
                }
            }
        }

        // Check whether an IP address is private or local
        private bool IsPrivateOrLocalIp(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();

            if (IPAddress.IsLoopback(ip) ||
                ip.Equals(IPAddress.Any) ||
                ip.Equals(IPAddress.IPv6Any) ||
                ip.IsIPv6LinkLocal ||
                ip.IsIPv6SiteLocal ||
                ip.IsIPv6Multicast)
            {
                return true;
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                byte[] bytes = ip.GetAddressBytes();
                return (bytes[0] & 0xFE) == 0xFC;
            }

            if (ip.AddressFamily != AddressFamily.InterNetwork)
                return true;

            byte[] b = ip.GetAddressBytes();

            return b[0] == 0 ||
                   b[0] == 10 ||
                   b[0] == 127 ||
                   (b[0] == 100 && b[1] >= 64 && b[1] <= 127) ||
                   (b[0] == 169 && b[1] == 254) ||
                   (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                   (b[0] == 192 && b[1] == 168) ||
                   (b[0] == 192 && b[1] == 0 && b[2] == 0) ||
                   (b[0] == 192 && b[1] == 0 && b[2] == 2) ||
                   (b[0] == 198 && (b[1] == 18 || b[1] == 19)) ||
                   (b[0] == 198 && b[1] == 51 && b[2] == 100) ||
                   (b[0] == 203 && b[1] == 0 && b[2] == 113) ||
                   b[0] >= 224;
        }

        // Load and check robots.txt rules
        private async Task LoadRobotsRulesAsync(Uri baseUri)
        {
            if (_robotsLoaded)
                return;

            _robotsLoaded = true;

            string robotsUrl =
                $"{baseUri.Scheme}://{baseUri.Authority}/robots.txt";

            try
            {
                await ValidatePublicUrlAsync(robotsUrl);

                using var response = await _httpClient.GetAsync(robotsUrl);

                // If robots.txt does not exist, crawling is allowed.
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return;

                // If robots.txt cannot be checked, stop crawling.
                if (!response.IsSuccessStatusCode)
                {
                    _blockAllCrawling = true;
                    return;
                }

                string content =
                    await response.Content.ReadAsStringAsync();

                bool insideWildcardGroup = false;

                foreach (string rawLine in content.Split('\n'))
                {
                    string line = rawLine.Split('#')[0].Trim();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split(':', 2);

                    if (parts.Length != 2)
                        continue;

                    string directive = parts[0].Trim();
                    string value = parts[1].Trim();

                    if (directive.Equals(
                        "User-agent",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        insideWildcardGroup = value == "*";
                    }
                    else if (insideWildcardGroup &&
                        directive.Equals(
                            "Disallow",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(value))
                            _robotsRules.Add((value, false));
                    }
                    else if (insideWildcardGroup &&
                        directive.Equals(
                            "Allow",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(value))
                            _robotsRules.Add((value, true));
                    }
                }
            }
            catch
            {
                // Do not crawl if robots.txt cannot be checked.
                _blockAllCrawling = true;
            }
        }

        // Check whether a page is allowed by robots.txt
        private bool IsAllowedByRobots(string url)
        {
            if (_blockAllCrawling)
                return false;

            Uri uri = new Uri(url);
            string path = uri.PathAndQuery;

            var matchingRules = _robotsRules
                .Where(rule => path.StartsWith(
                    rule.Path,
                    StringComparison.Ordinal))
                .ToList();

            if (matchingRules.Count == 0)
                return true;

            int longestMatchLength =
                matchingRules.Max(rule => rule.Path.Length);

            var bestRules = matchingRules
                .Where(rule => rule.Path.Length == longestMatchLength)
                .ToList();

            // Allow wins if Allow and Disallow rules have equal length.
            return bestRules.Any(rule => rule.Allow);
        }

        // Fetch website HTML with manual redirect handling
        public async Task<string> GetWebsiteHtmlAsync(string url)
        {
            const int maxRedirects = 5;

            string currentUrl = url;

            for (int i = 0; i <= maxRedirects; i++)
            {
                await ValidatePublicUrlAsync(currentUrl);

                using var response = await _httpClient.GetAsync(
                    currentUrl,
                    HttpCompletionOption.ResponseHeadersRead);

                int statusCode = (int)response.StatusCode;

                if (statusCode >= 300 && statusCode < 400)
                {
                    if (response.Headers.Location == null)
                    {
                        throw new HttpRequestException(
                            "Redirect location was not provided.");
                    }

                    if (i == maxRedirects)
                    {
                        throw new HttpRequestException(
                            "Maximum redirect limit exceeded.");
                    }

                    Uri currentUri = new Uri(currentUrl);

                    Uri redirectUri = response.Headers.Location.IsAbsoluteUri
                        ? response.Headers.Location
                        : new Uri(currentUri, response.Headers.Location);

                    currentUrl = redirectUri.ToString();

                    continue;
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }

            throw new HttpRequestException(
                "Unable to access the website.");
        }

        // Extract page title
        public string? GetPageTitle(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            return document.DocumentNode
                .SelectSingleNode("//title")?.InnerText.Trim();
        }

        // Extract meta description
        public string? GetMetaDescription(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            return document.DocumentNode
                .SelectSingleNode("//meta[@name='description']")
                ?.GetAttributeValue("content", null)?.Trim();
        }

        // Count H1 headings
        public int GetH1Count(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            return document.DocumentNode
                .SelectNodes("//h1")?.Count ?? 0;
        }

        // Extract canonical URL
        public string? GetCanonicalUrl(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var canonicalNode = document.DocumentNode
                .SelectSingleNode("//link[@rel='canonical']");

            string? canonicalUrl = canonicalNode?
                .GetAttributeValue("href", null)?.Trim();

            if (string.IsNullOrWhiteSpace(canonicalUrl))
                return null;

            if (Uri.TryCreate(
                canonicalUrl,
                UriKind.Absolute,
                out Uri? uri)
                && (uri.Scheme == Uri.UriSchemeHttp ||
                    uri.Scheme == Uri.UriSchemeHttps))
            {
                return canonicalUrl;
            }

            return null;
        }

        // Count JSON-LD structured data blocks
        public int GetJsonLdCount(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            return document.DocumentNode
                .SelectNodes("//script[@type='application/ld+json']")?.Count ?? 0;
        }

        // Check internal links using secure HttpClient
        public async Task<List<BrokenLink>> GetBrokenInternalLinksAsync(
            string html, string pageUrl)
        {
            var brokenLinks = new List<BrokenLink>();

            var document = new HtmlDocument();
            document.LoadHtml(html);

            var links = document.DocumentNode.SelectNodes("//a[@href]");

            if (links == null)
                return brokenLinks;

            var baseUri = new Uri(pageUrl);

            var uniqueUrls = links
                .Select(link => link.GetAttributeValue("href", ""))
                .Select(href =>
                    Uri.TryCreate(baseUri, href, out Uri? linkUri)
                        ? linkUri
                        : null)
                .Where(uri =>
                    uri != null &&
                    uri.Host == baseUri.Host &&
                    (uri.Scheme == "http" || uri.Scheme == "https"))
                .Select(uri => uri!.ToString())
                .Distinct()
                .Take(20)
                .ToList();

            using var semaphore = new SemaphoreSlim(5);

            var tasks = uniqueUrls.Select(async url =>
            {
                await semaphore.WaitAsync();

                try
                {
                    // Validate URL before making the request
                    await ValidatePublicUrlAsync(url);

                    using var response = await _httpClient.GetAsync(
                        url,
                        HttpCompletionOption.ResponseHeadersRead);

                    int statusCode = (int)response.StatusCode;

                    if (statusCode >= 400)
                    {
                        lock (brokenLinks)
                        {
                            brokenLinks.Add(new BrokenLink
                            {
                                Url = url,
                                FoundOnPage = pageUrl,
                                StatusCode = statusCode
                            });
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    lock (brokenLinks)
                    {
                        brokenLinks.Add(new BrokenLink
                        {
                            Url = url,
                            FoundOnPage = pageUrl,
                            StatusCode = 0
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    lock (brokenLinks)
                    {
                        brokenLinks.Add(new BrokenLink
                        {
                            Url = url,
                            FoundOnPage = pageUrl,
                            StatusCode = 0
                        });
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            return brokenLinks;
        }

        // Crawl a maximum of 5 website pages
        public async Task<List<CrawledPage>> CrawlWebsiteAsync(
            string startUrl)
        {
            var crawledPages = new List<CrawledPage>();
            var visitedUrls = new HashSet<string>();
            var urlsToVisit = new Queue<string>();

            var baseUri = new Uri(startUrl);

            // Check robots.txt before crawling.
            await LoadRobotsRulesAsync(baseUri);

            if (!IsAllowedByRobots(startUrl))
            {
                throw new HttpRequestException(
                    "Crawling this website is not allowed by robots.txt.");
            }

            urlsToVisit.Enqueue(startUrl);

            const int maxPages = 5;

            while (urlsToVisit.Count > 0 &&
                   crawledPages.Count < maxPages)
            {
                string currentUrl = urlsToVisit.Dequeue();

                if (!visitedUrls.Add(currentUrl))
                    continue;

                // Skip pages disallowed by robots.txt.
                if (!IsAllowedByRobots(currentUrl))
                    continue;

                // Wait one second between page requests.
                if (crawledPages.Count > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                try
                {
                    string html = await GetWebsiteHtmlAsync(currentUrl);

                    crawledPages.Add(new CrawledPage
                    {
                        Url = currentUrl,
                        HtmlContent = html
                    });

                    var document = new HtmlDocument();
                    document.LoadHtml(html);

                    var links = document.DocumentNode.SelectNodes("//a[@href]");

                    if (links == null)
                        continue;

                    foreach (var link in links)
                    {
                        string href = link.GetAttributeValue("href", "");

                        if (Uri.TryCreate(baseUri, href, out Uri? linkUri)
                            && linkUri.Host == baseUri.Host
                            && (linkUri.Scheme == "http" ||
                                linkUri.Scheme == "https"))
                        {
                            string nextUrl =
                                linkUri.GetLeftPart(UriPartial.Path);

                            if (!visitedUrls.Contains(nextUrl) &&
                                IsAllowedByRobots(nextUrl))
                            {
                                urlsToVisit.Enqueue(nextUrl);
                            }
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    if (currentUrl == startUrl)
                        throw;
                }
                catch (TaskCanceledException)
                {
                    if (currentUrl == startUrl)
                        throw;
                }
            }

            return crawledPages;
        }
    }
}
