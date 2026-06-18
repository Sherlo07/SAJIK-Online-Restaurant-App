using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OnlineRestaurantApp.Filters
{
    public class GlobalExceptionFilter: IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            // Log exception
            Console.WriteLine(context.Exception.Message);
            // Custom error response
            context.Result = new ViewResult
            {
                ViewName = "MyError"
            };
            context.ExceptionHandled = true;
        }
    }
}
