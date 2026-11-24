# SEO Audit Results

## Website selected
- URL: https://example.com
- Reason: Used as a stable, publicly accessible test site to verify the audit flow.

**Assignment note:** `example.com` is a demonstration website, not a product website. The brief asks for a product website. If possible, rerun the audit against an accessible product website and update this report.

## Results
- HTTP response: 200 OK
- Pages crawled: 1
- Overall SEO score: **25/100**

| Metric | Observed result | Status |
|---|---|---|
| Title | `Example Domain` — 14 characters; target 30–60 | Fail |
| Meta description | Missing | Fail |
| Broken internal links | 0 found | Pass |
| H1 structure | 0 headings; target exactly one per page | Fail |
| Canonical URL | Missing | Fail |
| Structured data | 0 JSON-LD blocks | Fail |

## Summary
The page was fetched successfully. It passed the broken internal links check because no broken internal links were found. The title was shorter than the target range. The meta description, H1 heading, canonical URL, and JSON-LD structured data were missing.

The crawler/API calculated the metric values and score. Gemini generated recommendations only; it did not calculate the score.

## Limitations
Only one page was crawled. Results may differ on larger websites. Some sites block automated crawling or disallow it through robots.txt.
