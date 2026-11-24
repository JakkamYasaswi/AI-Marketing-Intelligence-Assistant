# AI Marketing Intelligence Assistant

A React + ASP.NET Core application that crawls a website, checks five SEO metrics, calculates an SEO score, and uses Gemini to generate prioritized recommendations.

## Features
- Submit a website URL through the React interface.
- `POST /api/Audit` returns structured JSON.
- Crawls up to 5 same-host pages.
- Checks title/meta description, broken internal links, H1 count, canonical URL, and JSON-LD.
- Calculates the score in application code; Gemini only generates recommendations.
- Checks robots.txt, limits redirects, uses request timeouts, and blocks localhost/private IP destinations.
- Downloads the report as a PDF.

## Tech stack
C#, ASP.NET Core Web API, HtmlAgilityPack, Google.GenAI (Gemini), React, Vite, Recharts, React Markdown, jsPDF, and xUnit.

## Run the backend
1. Open the solution in Visual Studio.
2. Set the Gemini key using User Secrets (never commit the key):
   ```bash
   dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY" --project MarketingIntelligence.API
   ```
3. Run `MarketingIntelligence.API`.
4. Swagger: `https://localhost:7082/swagger`.

## Run the frontend
In `marketing-intelligence-frontend`:
```bash
npm install
npm run dev
```
Open `http://localhost:5173`. Keep the backend running; the frontend calls `https://localhost:7082/api/Audit`.

## Run tests
From the solution folder:
```bash
dotnet test
```

## API example
`POST /api/Audit`
```json
{ "url": "https://example.com" }
```

The response includes the URL, pages crawled, overall score, page findings, broken links, and Gemini recommendations.

## Limitations
The crawler is limited to 5 pages and up to 20 unique internal links per page. JavaScript-rendered content may not be detected. Some sites block automated requests or disallow crawling. The current test suite has 5 SEO extraction tests; add invalid-input and edge-case tests before claiming full assignment test coverage.
