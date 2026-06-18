using Microsoft.AspNetCore.Mvc.Filters;

namespace OnlineRestaurantApp.Filters
{
    public class ActionLogFilter:IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            Console.WriteLine("Action Started");
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            Console.WriteLine("Action Completed");
        }
    }
}
