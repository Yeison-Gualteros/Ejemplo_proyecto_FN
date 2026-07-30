using AutoMapper;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Shared.DataTransferObjects;
using Shared.DataTransferObjects.Documentos;
using Shared.DataTransferObjects.Permisos;
using Shared.DataTransferObjects.Roles;
using Shared.DataTransferObjects.User;


namespace FileNova
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ==========================
            // Usuarios
            // ==========================
            CreateMap<UserForRegistrationDto, User>()
                .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.Nombre))
                .ForMember(dest => dest.Apellido, opt => opt.MapFrom(src => src.Apellido))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(_ => 1)); // Activo por defecto


            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.Nombre))
                .ForMember(dest => dest.Apellido, opt => opt.MapFrom(src => src.Apellido))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado))
                .ForMember(dest => dest.Permisos, opt => opt.Ignore()) // lo llenamos en el service si aplica
                .ForMember(dest => dest.Rol, opt => opt.Ignore());     // lo llenamos en el service


            CreateMap<UserForUpdateDto, User>()
                .ForAllMembers(opts =>
                    opts.Condition((src, dest, srcMember) =>
                    {
                        if (srcMember == null)
                            return false;
                        if (srcMember is string str)
                            return !string.IsNullOrEmpty(str);
                        return true;
                    }));

            CreateMap<UserForUpdateFullDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore());

            // ==========================
            // Roles 
            // ==========================
            // Crear rol
            CreateMap<RolForCreationDto, Role>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.NormalizedName, opt => opt.MapFrom(src => src.Name.ToUpper()))
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()));

            // Actualizar rol
            CreateMap<RolForUpdateDto, Role>()
                .ForMember(dest => dest.NormalizedName, opt => opt.MapFrom(src => src.Name != null ? src.Name.ToUpper() : null))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // DTO para enviar al cliente
            CreateMap<Role, RolDto>()
                .ForMember(dest => dest.Permisos,
                    opt => opt.MapFrom(src =>
                        src.Rol_Permisos != null
                            ? src.Rol_Permisos.Select(rp => new PermisosDto
                            {
                                Id_Permiso = rp.Permiso.Id_Permiso,
                                Nombre = rp.Permiso.Nombre,
                                Source = "role"
                            })
                            : new List<PermisosDto>()));




            // ==========================
            // Documentos
            // ==========================
            // Crear Documento
            CreateMap<DocumentoForCreationDto, Documento>();
            // Mostrar Documentos
            CreateMap<Documento, DocumentoDTO>()
                .ForMember(dest => dest.VersionActual,
                    opt => opt.MapFrom(src => src.VersionActual))
                //.ForMember(dest => dest.VersionActual, opt => opt.Ignore())
                .ForMember(dest => dest.UsuarioSubio, opt => opt.MapFrom(src => (src.User != null) ? $"{src.User.Nombre} {src.User.Apellido}".Trim() : ""))

                .ForMember(dest => dest.Fecha_Creacion, opt => opt.MapFrom(src => src.Fecha_Creacion))
                .ForMember(dest => dest.Fecha_Modificacion, opt => opt.MapFrom(src => src.Fecha_Modificacion))

                // VERSION
                .ForMember(dest => dest.Estado,
                    opt => opt.MapFrom(src => src.VersionActual != null ? src.VersionActual.Estado : 0))

                .ForMember(dest => dest.Ruta,
                    opt => opt.MapFrom(src => src.VersionActual != null ? src.VersionActual.RutaPdf : null))

                .ForMember(dest => dest.Tipo,
                    opt => opt.MapFrom(src =>
                        src.TipoDocumento != null
                            ? src.TipoDocumento.Nombre
                            : ""))

                .ForMember(dest => dest.IdTipoDocumento,
                    opt => opt.MapFrom(src =>
                        src.TipoDocumento != null
                            ? src.TipoDocumento.IdTipoDocumento
                            : ""))

                // USUARIO
                .ForMember(dest => dest.UsuarioSubio,
                    opt => opt.MapFrom(src => src.User != null ? src.User.Nombre : ""))

                .ForMember(dest => dest.ApellidoUsuario,
                    opt => opt.MapFrom(src => src.User != null ? src.User.Apellido : ""))

                // APROBADOR
                .ForMember(dest => dest.AprobadorNombre,
                    opt => opt.MapFrom(src => src.Aprobador != null ? src.Aprobador.Nombre : ""))

                // CAMPOS NORMALES
                .ForMember(dest => dest.IdProceso,
                    opt => opt.MapFrom(src =>
                        src.Proceso != null 
                            ? src.Proceso.IdProceso : ""))

                .ForMember(dest => dest.Etiquetado,
                opt => opt.MapFrom(src => src.Etiquetado ?? ""))

                .ForMember(dest => dest.ContenidoJson, opt => opt.MapFrom(src => src.ContenidoJson))
                .ForMember(dest => dest.FirmasAprobacionJson, opt => opt.MapFrom(src => src.FirmasAprobacionJson));

            CreateMap<DocumentoVersion, DocumentoVersionDTO>();


            // Actualizar Documentos
            CreateMap<DocumentoForUpdateDto, Documento>()
                .ForMember(dest => dest.Consecutivo, opt => opt.Ignore()) // 🔥 CLAVE
                .ForMember(dest => dest.Id_Documento, opt => opt.Ignore()) // 🔒 seguridad
                .ForMember(dest => dest.VersionActualId, opt => opt.Ignore()) // 🔒 seguridad
                .ForAllMembers(opts =>
                    opts.Condition((src, dest, srcMember) =>
                    {
                        if (srcMember == null)
                            return false;

                        if (srcMember is string str)
                            return !string.IsNullOrWhiteSpace(str);

                        return true;
                    }));

            // Trazabilidad del Documento
            CreateMap<Trazabilidad_Documento, Trazabilidad_DocumentoDTO>()
                .ForMember(dest => dest.NombreUsuario,
                    opt => opt.MapFrom(src => src.User != null ? src.User.Nombre : "Sistema"))
                .ForMember(dest => dest.Rol,
                    opt => opt.MapFrom(src => src.Rol ?? "Sistema"));

            // ==========================
            // Permisos 
            // ==========================
            // Mostrar Permisos
            CreateMap<Permiso, PermisosDto>();
            // Crear Permisos
            CreateMap<PermisosForCreationDto, Permiso>();
            // Actualizar Permisos
            CreateMap<PermisoForUpdateDto, Permiso>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            // Asignar Permisos a un rol
            CreateMap<Rol_Permiso, PermisosDto>()
                .ForMember(dest => dest.Id_Permiso, opt => opt.MapFrom(src => src.Permiso.Id_Permiso))
                .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.Permiso.Nombre))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(_ => "role"));


        }
    }
}
