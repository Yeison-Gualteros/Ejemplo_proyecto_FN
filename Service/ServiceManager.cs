using AutoMapper;
using Contracts;
using Contracts.Interface;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Repository;
using Service.Contracts;
using Shared.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public sealed class ServiceManager : IServiceManager
    {
        private readonly Lazy<IDocumentoService> _documentoService;
        private readonly Lazy<ISolicitudService> _solicitudService;
        private readonly Lazy<ITrazabilidad_DocumentoService> _trazabilidad_DocumentoService;
        private readonly Lazy<IAuthenticationService> _authenticationService;
        private readonly Lazy<IRoleService> _rolService;
        private readonly Lazy<IPermisosService> _permisosService;
        private readonly Lazy<IUserService> _userService;
        private readonly RepositoryContext _repositoryContext;
        private readonly IEmailService _emailService;
        private readonly Lazy<ITipoDocumentoService> _tipoDocumentoService;
        private readonly Lazy<IProcesoService> _procesoService;

        public ServiceManager(
            IRepositoryManager repositoryManager,
            ILoggerManager logger,
            IMapper mapper,
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IOptions<JwtConfiguration> configuration,
            RepositoryContext repositoryContext,
            IEmailService emailService
            
            )
        {
            //_emailService = new Lazy<IEmailService>(() =>
            //    new EmailService());
            _repositoryContext = repositoryContext;
            _emailService = emailService;

            _documentoService = new Lazy<IDocumentoService>(() =>
                new DocumentoService(repositoryManager, logger, mapper, permisosService, Trazabilidad_DocumentoService, userManager, emailService));

            _solicitudService = new Lazy<ISolicitudService>(() =>
                new SolicitudService(repositoryManager, logger, mapper));

            _trazabilidad_DocumentoService = new Lazy<ITrazabilidad_DocumentoService>(() =>
                new Trazabilidad_DocumentoService(repositoryManager, logger, mapper, userManager));

            _authenticationService = new Lazy<IAuthenticationService>(() =>
                new AuthenticationService(logger, mapper, userManager, configuration, repositoryContext, _emailService));

            _rolService = new Lazy<IRoleService>(() =>
                new RolService(repositoryManager, mapper, logger, userManager));

            _permisosService = new Lazy<IPermisosService>(() =>
                new PermisosService(repositoryManager, logger, mapper, userManager));
            

            _userService = new Lazy<IUserService>(() =>
                new UserService(
                    repositoryManager,
                    mapper,
                    logger,
                    userManager,
                    roleManager,
                    _repositoryContext,
                    _permisosService.Value,
                    _emailService

                ));

            _tipoDocumentoService = new Lazy<ITipoDocumentoService>(() =>
                new TipoDocumentoService(repositoryManager));
                _procesoService = new Lazy<IProcesoService>(() => new ProcesoService(repositoryManager));
        }
        public IDocumentoService DocumentoService => _documentoService.Value;
        public ISolicitudService SolicitudService => _solicitudService.Value;
        public ITrazabilidad_DocumentoService Trazabilidad_DocumentoService => _trazabilidad_DocumentoService.Value;
        public IAuthenticationService AuthenticationService => _authenticationService.Value;
        public IRoleService RoleService => _rolService.Value;
        public IPermisosService permisosService => _permisosService.Value;
        public IUserService UserService => _userService.Value;
        public IEmailService EmailService => _emailService;
        public ITipoDocumentoService TipoDocumentoService => _tipoDocumentoService.Value;
        public IProcesoService ProcesoService => _procesoService.Value;


    }
}
