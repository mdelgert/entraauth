using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using shared.Models;

namespace frontend.Data
{
    public class WeatherForecastService
    {
        private const string ServiceName = "DownstreamApi";
        private readonly IDownstreamApi _downstreamApi;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;
        private readonly NavigationManager _navigationManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WeatherForecastService> _logger;

        public WeatherForecastService(
            IDownstreamApi downstreamApi,
            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
            NavigationManager navigationManager,
            IConfiguration configuration,
            ILogger<WeatherForecastService> logger)
        {
            _downstreamApi = downstreamApi ?? throw new ArgumentNullException(nameof(downstreamApi));
            _consentHandler = consentHandler ?? throw new ArgumentNullException(nameof(consentHandler));
            _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            try
            {
                _logger.LogInformation("Calling WeatherForecast API endpoint.");
                var baseUrl = _configuration["BackendApi"] ?? throw new InvalidOperationException("BackendApi configuration is missing.");

                var forecasts = await _downstreamApi.GetForUserAsync<WeatherForecast[]>(
                    ServiceName,
                    options => options.RelativePath = "/WeatherForecast");

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
                _logger.LogInformation("Calling WeatherForecastEntra API endpoint.");

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