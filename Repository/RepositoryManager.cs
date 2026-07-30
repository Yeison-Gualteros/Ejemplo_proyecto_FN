using Contracts;
using Contracts.Interface;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Repository.Clases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly RepositoryContext _repositoryContext;
        private readonly Lazy<IDocumentoRepository> _documentoRepository;
        private readonly Lazy<ISolicitudRepository> _solicitudRepository;
        private readonly Lazy<ITrazabilidad_DocumentoRepository> _trazabilidad_DocumentoRepository;
        private readonly Lazy<IRoleRepository> _roleRepository;
        private readonly Lazy<IPermisosRepository> _permisosRepository;
        private readonly Lazy<IUserRepository> _usersRepository;
        private readonly Lazy<IUserPermisosRepository> _userPermisosRepository;
        private readonly Lazy<IRol_PermisosRepository> _rol_PermisosRepository;
        private readonly Lazy<IDocumentoVersionRepository> _documentoVersionRepository;
        private readonly Lazy<ITipoDocumentoRepository> _tipoDocumentoRepository;
        private readonly Lazy<IProcesoRepository> _procesoRepository;

        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;


        public RepositoryManager(RepositoryContext repositoryContext, UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _repositoryContext = repositoryContext;
            _documentoRepository = new Lazy<IDocumentoRepository>(() => new DocumentoRepository(repositoryContext));
            _solicitudRepository = new Lazy<ISolicitudRepository>(() => new SolicitudRepository(repositoryContext));
            _trazabilidad_DocumentoRepository = new Lazy<ITrazabilidad_DocumentoRepository>(() => new Trazabilidad_DocumentoRepository(repositoryContext));
            _roleRepository = new Lazy<IRoleRepository>(() => new RoleRepository(repositoryContext));
            _permisosRepository = new Lazy<IPermisosRepository>(() => new PermisosRepository(repositoryContext));
            _usersRepository = new Lazy<IUserRepository>(() => new UserRepository(repositoryContext, userManager, roleManager));
            _userPermisosRepository = new Lazy<IUserPermisosRepository>(() => new UserPermisosRepository(repositoryContext));
            _rol_PermisosRepository = new Lazy<IRol_PermisosRepository>(() => new Rol_PermisoRepository(repositoryContext));
            _documentoVersionRepository = new Lazy<IDocumentoVersionRepository>(() => new DocumentoVersionRepository(repositoryContext));
            _tipoDocumentoRepository = new Lazy<ITipoDocumentoRepository>(() => new TipoDocumentoRepository(repositoryContext));
            _procesoRepository = new Lazy<IProcesoRepository>(() => new ProcesoRepository(repositoryContext));
        }

        public IDocumentoRepository Documento => _documentoRepository.Value;
        public ISolicitudRepository Solicitud => _solicitudRepository.Value;
        public ITrazabilidad_DocumentoRepository trazabilidad_Documento => _trazabilidad_DocumentoRepository.Value;
        public IRoleRepository Role => _roleRepository.Value;
        public IPermisosRepository Permisos => _permisosRepository.Value;
        public IUserRepository User => _usersRepository.Value;
        public IUserPermisosRepository UserPermisos => _userPermisosRepository.Value;
        public IRol_PermisosRepository Rol_Permisos => _rol_PermisosRepository.Value;
        public IDocumentoVersionRepository DocumentoVersion => _documentoVersionRepository.Value;
        public ITipoDocumentoRepository TipoDocumento => _tipoDocumentoRepository.Value;
        public IProcesoRepository Proceso => _procesoRepository.Value;

        public async Task SaveAsync()
        {
            await _repositoryContext.SaveChangesAsync();
        }
    }
}