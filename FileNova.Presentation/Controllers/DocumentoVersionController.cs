using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileNova.Presentation.Controllers
{
    [Route("api/documentoversiones")]
    [ApiController]
    public class DocumentoVersionController : ControllerBase
    {

        private readonly IServiceManager _service;
        private readonly IServiceManager _serviceManager;


        public DocumentoVersionController(IServiceManager service, IServiceManager serviceManager)
        {
            _service = service;
            _serviceManager = serviceManager;

        }


        
    }
}
