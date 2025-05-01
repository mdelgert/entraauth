using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace frontend.Data
{
    public class WeatherForecastService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WeatherForecastService> _logger;

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public WeatherForecastService(IConfiguration configuration, ILogger<WeatherForecastService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task<WeatherForecast[]> GetForecastAsync(DateOnly startDate)
        {
            return Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray());
        }

        public async Task<WeatherForecast[]> GetForecastFromApiAsync()
        {
            _logger.LogInformation("Calling WeatherForecast API endpoint.");

            using var httpClient = new HttpClient();
            var baseUrl = _configuration["BackendApi"];
            var response = await httpClient.GetFromJsonAsync<WeatherForecast[]>($"{baseUrl}WeatherForecast");

            _logger.LogInformation("WeatherForecast API call completed.");

            return response ?? Array.Empty<WeatherForecast>();
        }
    }
}
