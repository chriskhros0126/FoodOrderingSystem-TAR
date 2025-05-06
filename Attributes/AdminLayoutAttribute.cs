using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodOrderingSystem.Attributes
{
    public class AdminLayoutAttribute : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ViewResult viewResult)
            {
                viewResult.ViewData["Layout"] = "~/Views/Shared/_AdminLayout.cshtml";
            }
        }
    }
} 