using Hangfire.Dashboard;

namespace SideCar.Business.Helpers.Utilities
{
    public class DevDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }
}
