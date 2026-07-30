using Microsoft.AspNetCore.Mvc.Filters;

namespace FileNova.Filter
{
    public abstract class ActionFilterAttribute : Attribute, IActionFilter,
        IFilterMetadata
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            throw new NotImplementedException();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            throw new NotImplementedException();
        }
    }

}
