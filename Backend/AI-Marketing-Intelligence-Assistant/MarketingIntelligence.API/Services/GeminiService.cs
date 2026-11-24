
using Google.GenAI;
using Microsoft.Extensions.Options;
using MarketingIntelligence.API.Models;

namespace MarketingIntelligence.API.Services
{
    public class GeminiService
    {
        private readonly Client _client;

        public GeminiService(IOptions<GeminiSettings> settings)
        {
            _client = new Client(
                apiKey: settings.Value.ApiKey
            );
        }

        public async Task<string> GenerateRecommendationsAsync(string prompt)
        {
            var response = await _client.Models.GenerateContentAsync(
                model: "gemini-3.5-flash-lite", 
                contents: prompt
            );

            return response.Candidates[0].Content.Parts[0].Text;
        }
    }
}
