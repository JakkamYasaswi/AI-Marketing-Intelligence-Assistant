# AI Usage

## Tools Used

- **ChatGPT:** Used for understanding the assignment, planning the implementation, learning C#/.NET and React concepts, generating and reviewing code, troubleshooting errors, and preparing documentation.
- **Gemini API:** Used in the application to convert SEO audit findings into short, prioritized recommendations for website improvement.

## AI's Role in the Application

The website crawler extracts SEO information, and the application calculates the overall SEO score. Gemini receives the audit findings and generates recommendations based on the results.

The SEO metric values and overall score are calculated by the application, not by the LLM.

## Verification

I treated AI-generated suggestions as guidance and verified the implementation by building and running the application.

I tested the application through Swagger and the React frontend using different inputs:

- Valid website URL: `https://example.com`
- Invalid URL scheme: `ftp://example.com`
- Localhost URL: `http://localhost:5000`
- Website that disallows crawling through robots.txt: `https://www.wikipedia.org`

I checked the API responses and confirmed that valid requests worked and invalid or restricted requests were rejected appropriately.

## Two Examples of Implementation Issues Identified and Fixed

1. **Frontend and backend communication:** Initially, the React frontend could not communicate successfully with the backend because the CORS configuration was missing or incomplete. I identified the issue during API integration, updated the CORS configuration in `Program.cs`, and verified that the frontend could successfully call the backend API.

2. **Frontend build issue:** During frontend development, the Vite build failed because of duplicate CSS selectors. I identified the issue from the build error, corrected the CSS, and verified that the frontend worked successfully.

## Work I Checked Directly

I tested the API through Swagger and verified the SEO audit results in the React frontend. I also checked that the crawler calculates the SEO metrics and overall score independently of Gemini.

## What I Would Improve With One More Day

- Add more automated tests for invalid inputs and edge cases.
- Test the application with an accessible product website and a larger multi-page website.
- Improve robots.txt handling and test redirects, timeouts, and network failures.
- Improve the accuracy and coverage of SEO checks.