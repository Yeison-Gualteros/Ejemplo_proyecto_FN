using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Repository.Configuration;

namespace Repository
{
    public class RepositoryContext : IdentityDbContext<User, Role, string>
    {
        public RepositoryContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapear tablas
            modelBuilder.Entity<User>().ToTable("User"); // AspNetUsers -> User
            modelBuilder.Entity<Role>().ToTable("Role"); // AspNetRoles -> Role
            //modelBuilder.Entity<User_Role>().ToTable("User_Role"); // AspNetUserRoles -> User_Role

            // Usuario - Documentos
            modelBuilder.Entity<Documento>()
                .HasOne(d => d.User)
                .WithMany(u => u.Documentos)
                .HasForeignKey(d => d.Id_Usuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Documento>()
                .HasMany(d => d.Versiones)
                .WithOne(v => v.Documento)
                .HasForeignKey(v => v.Id_Documento)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Documento>()
                .HasOne(d => d.VersionActual)
                .WithMany()
                .HasForeignKey(d => d.VersionActualId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Documento>()
                .HasOne(d => d.TipoDocumento)
                .WithMany(t => t.Documentos)
                .HasForeignKey(d => d.IdTipoDocumento)
                .OnDelete(DeleteBehavior.Restrict);

            // Documento - Trazabilidad
            modelBuilder.Entity<Trazabilidad_Documento>()
                .HasOne(t => t.Documento)
                .WithMany(d => d.Trazabilidad_Documentos)
                .HasForeignKey(t => t.Id_Documento)
                .OnDelete(DeleteBehavior.Cascade);

            // Usuario - Trazabilidad
            modelBuilder.Entity<Trazabilidad_Documento>()
                .HasOne(t => t.User)
                .WithMany(u => u.Trazabilidad_Documentos)
                .HasForeignKey(t => t.Id_Usuario)
                .OnDelete(DeleteBehavior.Restrict);

            // Usuario - Solicitud
            modelBuilder.Entity<Solicitud>()
                .HasOne(s => s.User)
                .WithMany(u => u.Solicitudes)
                .HasForeignKey(s => s.Id_Usuario)
                .OnDelete(DeleteBehavior.Restrict);

            // User_Permiso
            modelBuilder.Entity<User_Permiso>()
                .HasKey(up => new { up.UserId, up.Id_Permiso });

            modelBuilder.Entity<User_Permiso>()
                .HasOne(up => up.User)
                .WithMany(u => u.User_Permisos)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<User_Permiso>()
                .HasOne(up => up.Permiso)
                .WithMany(p => p.User_Permisos)
                .HasForeignKey(up => up.Id_Permiso);

            // Rol_Permiso
            modelBuilder.Entity<Rol_Permiso>()
                .HasKey(rp => new { rp.Id_Rol, rp.Id_Permiso });

            modelBuilder.Entity<Rol_Permiso>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.Rol_Permisos)
                .HasForeignKey(rp => rp.Id_Rol);

            modelBuilder.Entity<Rol_Permiso>()
                .HasOne(rp => rp.Permiso)
                .WithMany(p => p.Rol_Permisos)
                .HasForeignKey(rp => rp.Id_Permiso);

            modelBuilder.Entity<TipoDocumento>()
                .HasKey(t => t.IdTipoDocumento);

            modelBuilder.Entity<Proceso>()
                .HasKey(p => p.IdProceso);

            // Configuraciones adicionales
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(RepositoryContext).Assembly);
        }

        // DbSets
        public DbSet<Documento> Documentos { get; set; }
        public DbSet<Trazabilidad_Documento> Trazabilidad_Documentos { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<Rol_Permiso> Rol_Permisos { get; set; }
        public DbSet<Solicitud> Solicitudes { get; set; }
        public DbSet<User_Permiso> user_Permisos { get; set; }
        public DbSet<DocumentoVersion> DocumentoVersion { get; set; }
        public DbSet<TipoDocumento> TipoDocumentos { get; set; }
        public DbSet<Proceso> Procesos { get; set; }

    }
}