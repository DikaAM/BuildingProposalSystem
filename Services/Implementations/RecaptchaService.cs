using BuildingProposalSystem.Models;
using BuildingProposalSystem.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BuildingProposalSystem.Services.Implementations
{
    public class RecaptchaService : IRecaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly RecaptchaSettings _settings;

        public RecaptchaService(HttpClient httpClient, IOptions<RecaptchaSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<bool> VerifyAsync(string recaptchaToken)
        {
            if (string.IsNullOrWhiteSpace(recaptchaToken))
            {
                return false;
            }

            var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"secret", _settings.SecretKey },
                    {"response", recaptchaToken }
                }));

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Success ?? false;
        }

        private class RecaptchaVerifyResponse
        {
            public bool Success { get; set; }
        }
    }

}
