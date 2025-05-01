using shared.Models;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.Identity.Web;
using frontend.Pages.ToDoPages;

namespace frontend.Data
{
    public class WeatherForecastService
    {
        const string ServiceName = "DownstreamApi";
        [Inject] IDownstreamApi DownstreamApi { get; set; }
        [Inject] MicrosoftIdentityConsentAndConditionalAccessHandler ConsentHandler { get; set; }
        [Inject] NavigationManager Navigation { get; set; }

        private readonly IConfiguration _configuration;
        private readonly ILogger<WeatherForecastService> _logger;

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
                Summary = WeatherForecastSummaries.SummaryType[Random.Shared.Next(WeatherForecastSummaries.SummaryType.Length)]
            }).ToArray());
        }

        public async Task<WeatherForecast[]> GetForecastFromApiAsync()
        {
            _logger.LogInformation("Calling WeatherForecast API endpoint.");

            using var httpClient = new HttpClient();
            var baseUrl = _configuration["BackendApi"];
            var response = await httpClient.GetFromJsonAsync<WeatherForecast[]>($"{baseUrl}/WeatherForecast");

            _logger.LogInformation("WeatherForecast API call completed.");

            return response ?? Array.Empty<WeatherForecast>();
        }

        public async Task<WeatherForecast[]> GetForecastFromEntraAsync()
        {
            _logger.LogInformation("Calling WeatherForecast API endpoint.");

            using var httpClient = new HttpClient();
            var baseUrl = _configuration["BackendApi"];

            var response = await httpClient.GetFromJsonAsync<WeatherForecast[]>($"{baseUrl}/WeatherForecastEntra");

            //var weatherForecastList = (await DownstreamApi.GetForUserAsync<IEnumerable<ToDo>>(
            //        ServiceName,
            //        options => options.RelativePath = "/WeatherForecastEntra"))!;

            _logger.LogInformation("WeatherForecast API call completed.");

            return response ?? Array.Empty<WeatherForecast>();
        }
    }
}
