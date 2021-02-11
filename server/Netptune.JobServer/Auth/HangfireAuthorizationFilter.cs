using Hangfire.Dashboard;

namespace Netptune.JobServer.Auth
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;

            if (isAuthenticated) return true;

            httpContext.Response.Redirect("/account/login");

            return true;
        }
    }
}
