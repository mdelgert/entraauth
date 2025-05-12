using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
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


//https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=csharp

// The client credentials flow requires that you request the
// /.default scope, and pre-configure your permissions on the
// app registration in Azure. An administrator must grant consent
// to those permissions beforehand.
var scopes = new[] { "https://graph.microsoft.com/.default" };

// Values from app registration
var clientId = configuration.GetValue<string>("AzureAd:ClientId");
var tenantId = configuration.GetValue<string>("AzureAd:TenantId");

// Retrieve the ClientSecret from the ClientCredentials array
var clientSecret = configuration.GetSection("AzureAd:ClientCredentials")
    .GetChildren()
    .FirstOrDefault(c => c.GetValue<string>("SourceType") == "ClientSecret")
    ?.GetValue<string>("ClientSecret");

// Log the clientId, tenantId, and clientSecret
logger.LogInformation($"ClientId: {clientId} TenantId: {tenantId} ClientSecret: {clientSecret}");

// using Azure.Identity;
var options = new ClientSecretCredentialOptions
{
    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
};

// https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);

var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

//var result = await graphClient.Users.GetAsync();

//// Loop through all users and log them
//foreach (var user in result.Value)
//{
//    logger.LogInformation($"User: {user.DisplayName} - {user.Id}");
//}

//https://learn.microsoft.com/en-us/graph/api/user-post-users?view=graph-rest-1.0&tabs=csharp

var emailDoamain = configuration.GetValue<string>("EmailDomain");

// Log email domain
logger.LogInformation($"Email Domain: {emailDoamain}");

var requestBody = new User
{
    AccountEnabled = true,
    DisplayName = "Adele1 Vance",
    MailNickname = "AdeleV",
    //UserPrincipalName = "AdeleV@contoso.com",
    UserPrincipalName = "AdeleV1@" + emailDoamain,
    PasswordProfile = new PasswordProfile
    {
        ForceChangePasswordNextSignIn = true,
        Password = "xWwvJ]6NMw+bWH-d",
    },
};

// To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=csharp
var result = await graphClient.Users.PostAsync(requestBody);

logger.LogInformation("End:");

Console.ReadKey();