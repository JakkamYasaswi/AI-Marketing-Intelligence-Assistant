# Decisions

## Assumptions and rationale
1. Crawl a maximum of 5 same-host pages to keep requests bounded.
2. Treat links on the same host as internal; check up to 20 unique internal links per page.
3. Treat HTTP 4xx/5xx as broken. Record request failures/timeouts with status code `0`.
4. Require exactly one H1 heading per page.
5. Count a valid absolute HTTP/HTTPS canonical URL as present.
6. Count `application/ld+json` script blocks as structured data.
7. Use a 100-point score: title/meta 30, broken links 25, H1 15, canonical 15, JSON-LD 15. The application calculates the score; Gemini does not.
8. Respect robots.txt and stop when the requested page is disallowed.

## Ambiguities and resolutions
- The brief leaves the three extra metrics to the developer. I selected H1 structure, canonical URL, and JSON-LD because they are measurable from HTML and relevant to SEO.
- The brief does not define score weights. I chose fixed weights totaling 100.
- The brief does not define crawl size. I chose a 5-page, same-host limit.
- The brief does not define how to report network failures during link checks. I record them with status code `0`.

## Three choices to revisit
1. Improve robots.txt parsing to support more of the robots exclusion protocol.
2. Review metric weights and thresholds with an SEO specialist.
3. Add more tests for invalid URLs, robots.txt, redirects, timeouts, and broken-link edge cases.
