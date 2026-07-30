using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileNova.Presentation.Filters
{
    public class PatchValidationFilterAttribute : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Buscar cualquier parámetro que sea un JsonPatchDocument
            var patchDoc = context.ActionArguments
                .FirstOrDefault(a => a.Value is JsonPatchDocument).Value;

            if (patchDoc == null)
            {
                context.Result = new BadRequestObjectResult("patchDoc object is null");
                return;
            }

            // Validar el ModelState
            if (!context.ModelState.IsValid)
            {
                context.Result = new UnprocessableEntityObjectResult(context.ModelState);
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
