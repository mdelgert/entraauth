using shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace frontendapp.Data
{
    public class WeatherForecastService
    {
        private const string ServiceName = "DownstreamApi";
        private readonly IDownstreamApi _downstreamApi;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;
        private readonly NavigationManager _navigationManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WeatherForecastService> _logger;
        private readonly HttpClient _httpClient;

        public WeatherForecastService(
            IDownstreamApi downstreamApi,
            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
            NavigationManager navigationManager,
            IConfiguration configuration,
            ILogger<WeatherForecastService> logger,
            HttpClient httpClient)
        {
            _downstreamApi = downstreamApi ?? throw new ArgumentNullException(nameof(downstreamApi));
            _consentHandler = consentHandler ?? throw new ArgumentNullException(nameof(consentHandler));
            _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<WeatherForecast[]> GetForecastAsync(DateOnly startDate)
        {
            _logger.LogInformation("Calling WeatherForecast.");

            return Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = WeatherForecastSummaries.SummaryType[Random.Shared.Next(WeatherForecastSummaries.SummaryType.Length)]
            }).ToArray());
        }

        public async Task<WeatherForecast[]> GetForecastFromApiAsync()
        {
            try
            {
                _logger.LogInformation("Calling WeatherForecast insecure API endpoint.");

                var baseUrl = _configuration["DownstreamApi:BaseUrl"] ?? throw new InvalidOperationException("DownstreamApi:BaseUrl configuration is missing.");

                // Do a direct rest call to the API endpoint without using the downstream API client

                var response = await _httpClient.GetAsync(baseUrl + "/WeatherForecast");

                var forecasts = await _httpClient.GetFromJsonAsync<WeatherForecast[]>(baseUrl + "/WeatherForecast");

                _logger.LogInformation("WeatherForecast API call completed.");

                return forecasts ?? Array.Empty<WeatherForecast>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling WeatherForecast API.");
                _consentHandler.HandleException(ex);
                return Array.Empty<WeatherForecast>();
            }
        }

        public async Task<WeatherForecast[]> GetForecastFromEntraAsync()
        {
            try
            {
                _logger.LogInformation("Calling WeatherForecastEntra secure API endpoint.");

                var forecasts = await _downstreamApi.GetForUserAsync<WeatherForecast[]>(
                    ServiceName,
                    options => options.RelativePath = "/WeatherForecastEntra");

                _logger.LogInformation("WeatherForecastEntra API call completed.");
                return forecasts ?? Array.Empty<WeatherForecast>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling WeatherForecastEntra API.");
                _consentHandler.HandleException(ex);
                return Array.Empty<WeatherForecast>();
            }
        }
    }
}