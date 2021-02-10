using Hangfire.Dashboard;

namespace Netptune.JobServer
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;

            if (isAuthenticated) return true;

            httpContext.Response.Redirect("/identity/account/login");

            return true;
        }
    }
}
