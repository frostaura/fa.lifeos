using Hangfire.Dashboard;

namespace LifeOS.Api.Middleware;

/// <summary>
/// Basic authentication filter for Hangfire dashboard in production
/// </summary>
public class HangfireBasicAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // In production, require authentication
        // For now, check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            // Check for Basic auth header
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
            {
                SetUnauthorizedResponse(httpContext);
                return false;
            }

            try
            {
                var credentials = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(authHeader.Substring(6)));
                var parts = credentials.Split(':');
                
                if (parts.Length != 2)
                {
                    SetUnauthorizedResponse(httpContext);
                    return false;
                }

                var username = parts[0];
                var password = parts[1];

                // Use environment variables for credentials
                var expectedUsername = Environment.GetEnvironmentVariable("HANGFIRE_USERNAME") ?? "admin";
                var expectedPassword = Environment.GetEnvironmentVariable("HANGFIRE_PASSWORD") ?? "Admin123!";

                if (username != expectedUsername || password != expectedPassword)
                {
                    SetUnauthorizedResponse(httpContext);
                    return false;
                }
            }
            catch
            {
                SetUnauthorizedResponse(httpContext);
                return false;
            }
        }

        return true;
    }

    private void SetUnauthorizedResponse(HttpContext httpContext)
    {
        httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
        httpContext.Response.StatusCode = 401;
    }
}
