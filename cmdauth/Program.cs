using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Determine environment (defaulting to Development)
string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

// Build configuration
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Set up dependency injection
var services = new ServiceCollection();

// Add logging first
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Build service provider after all services are registered
var serviceProvider = services.BuildServiceProvider();

// Get the logger after the provider is built
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Begin:");

// Get environment form appsettings
var env = configuration.GetValue<string>("Environment");

// Log the environment
logger.LogInformation($"Environment: {env}");

logger.LogInformation("End:");

Console.ReadKey();