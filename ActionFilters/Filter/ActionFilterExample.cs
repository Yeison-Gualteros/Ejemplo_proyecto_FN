using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ActionFilters.Filter
{
    public class ActionFilterExample : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Lógica a ejecutar después de que la acción del controlador se haya ejecutado
        }
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Lógica a ejecutar antes de que la acción del controlador se ejecute
        }
    }
}
