using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Identity.Web.Resource;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Authorization
{
    public class ScopeAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly IConfiguration _configuration;

        public ScopeAuthorizationFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Skip for AllowAnonymous endpoints
            if (context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType().Name == "AllowAnonymousAttribute"))
            {
                return;
            }

            var httpMethod = context.HttpContext.Request.Method;
            var scopes = httpMethod == "GET" || httpMethod == "HEAD" || httpMethod == "OPTIONS"
                ? _configuration["AzureAD:Scopes:Read"]?.Split(' ')
                : _configuration["AzureAD:Scopes:Write"]?.Split(' ');

            var appPermissions = httpMethod == "GET" || httpMethod == "HEAD" || httpMethod == "OPTIONS"
                ? _configuration["AzureAD:AppPermissions:Read"]?.Split(' ')
                : _configuration["AzureAD:AppPermissions:Write"]?.Split(' ');

            if (scopes == null && appPermissions == null)
            {
                return;
            }

            try
            {
                var result = BuildRequiredScopeOrAppPermission(
                    context.HttpContext,
                    new RequiredScopeOrAppPermissionAttribute
                    {
                        AcceptedScope = scopes,
                        AcceptedAppPermission = appPermissions
                    });

                if (!result)
                {
                    context.Result = new ForbidResult();
                }
            }
            catch
            {
                context.Result = new ForbidResult();
            }
        }

        private bool BuildRequiredScopeOrAppPermission(
            Microsoft.AspNetCore.Http.HttpContext httpContext,
            RequiredScopeOrAppPermissionAttribute attribute)
        {
            // Check if token has app permissions
            var hasAppRole = false;
            if (attribute.AcceptedAppPermission != null)
            {
                hasAppRole = httpContext.User.Claims
                    .Any(c => c.Type == "roles" && attribute.AcceptedAppPermission.Contains(c.Value));
            }

            // Check if token has delegated permissions (scopes)
            var hasScope = false;
            if (attribute.AcceptedScope != null)
            {
                var scopeClaim = httpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/scope" || c.Type == "scp");

                if (scopeClaim != null)
                {
                    var scopes = scopeClaim.Value.Split(' ');
                    hasScope = attribute.AcceptedScope.Any(scope => scopes.Contains(scope));
                }
            }

            return hasAppRole || hasScope;
        }
    }
}