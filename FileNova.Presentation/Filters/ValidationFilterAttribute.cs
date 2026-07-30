using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileNova.Presentation.Filters
{
    public class ValidationFilterAttribute : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var action = context.RouteData.Values["action"];
            var controller = context.RouteData.Values["controller"];

            // Si el método NO tiene body, no validar
            if (context.HttpContext.Request.Method == HttpMethods.Get ||
                context.HttpContext.Request.Method == HttpMethods.Delete)
            {
                return;
            }

            // Buscar SOLO parámetros que sean Dto
            var dto = context.ActionArguments
                .FirstOrDefault(p => p.Value != null && p.Value.GetType().Name.EndsWith("Dto"))
                .Value;

            if (dto == null)
            {
                return; 
            }

            if (!context.ModelState.IsValid)
            {
                context.Result = new UnprocessableEntityObjectResult(context.ModelState);
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
