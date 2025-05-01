using shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web.Resource;

namespace backend.Controllers
{
    // Does not require scope or app just verifies a use in entra
    [Authorize]

    // Scope only
    //[RequiredScopeOrAppPermission(RequiredScopesConfigurationKey = "AzureAD:Scopes:Read")]
    
    // Read only scope
    //[RequiredScopeOrAppPermission(
    //    RequiredScopesConfigurationKey = "AzureAD:Scopes:Read",
    //    RequiredAppPermissionsConfigurationKey = "AzureAD:AppPermissions:Read"
    //)]

    // Read write
    //[RequiredScopeOrAppPermission(
    //    RequiredScopesConfigurationKey = "AzureAD:Scopes:Write",
    //    RequiredAppPermissionsConfigurationKey = "AzureAD:AppPermissions:Write"
    //)]

    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastEntraController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastEntraController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecastEntra")]
        public IEnumerable<WeatherForecast> Get()
        {
            if (HttpContext.User.Claims.Any(c => c.Type == "name"))
            {
                // Log the name that accessed the API
                _logger.LogInformation($"WeatherForecastEntraController accessed by {HttpContext.User.Claims.First(c => c.Type == "name").Value}");
            }

            if (HttpContext.User.Claims.Any(c => c.Type == "preferred_username")) //This is email address
            {
                // Log the preferred_username that accessed the API
                _logger.LogInformation($"WeatherForecastEntraController accessed by {HttpContext.User.Claims.First(c => c.Type == "preferred_username").Value}");
            }

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = WeatherForecastSummaries.SummaryType[Random.Shared.Next(WeatherForecastSummaries.SummaryType.Length)]
            })
            .ToArray();
        }
    }
}
