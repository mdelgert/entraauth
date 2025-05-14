//https://github.com/Azure-Samples/active-directory-dotnet-external-identities-api-connector-azure-function-validate/blob/master/SignUpValidation.cs

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using workflowconnector.Services;

namespace workflowconnector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly DbService _context;
        private readonly ILogger<LoginController> _logger;

        public LoginController(DbService context, ILogger<LoginController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet(Name = "GetLogin")]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogInformation("Login requested");

                var log = new Models.LogModel
                {
                    LogLevel = "Info",
                    Message = "Login requested"
                };

                await _context.Logs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while logging the login request");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }

            return Ok("Hello World!");
        }
    }
}
