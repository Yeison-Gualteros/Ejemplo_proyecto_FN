using Entities.Links;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FileNova.Presentation.Controllers
{
    [Route("api")]
    [ApiController]
    public class RootControllers : ControllerBase
    {
        private readonly LinkGenerator _linkGenerator;

        public RootControllers(LinkGenerator linkGenerator)
        {
            _linkGenerator = linkGenerator;
        }

        [HttpGet(Name = "GetRoot")]
        public IActionResult GetRoot()
        {
            var list = new List<Link>
            {
                new Link
                {
                    Href = _linkGenerator.GetUriByName(HttpContext, nameof(GetRoot), new { }),
                    Rel = "self",
                    Method = "GET"
                },
                new Link
                {
                    Href = _linkGenerator.GetUriByName(HttpContext, "GetDocumentos", new { }),
                    Rel = "documentos",
                    Method = "GET"
                },
                new Link
                {
                    Href = _linkGenerator.GetUriByName(HttpContext, "CreateDocumento", new { }),
                    Rel = "create_documento",
                    Method = "POST"
                },
                new Link
                {
                    Href = _linkGenerator.GetUriByName(HttpContext, "GetDocumetnosOptions", new { }),
                    Rel = "options_documento",
                    Method = "OPTIONS"
                }
            };

            return Ok(list); 
        }
    }
}
