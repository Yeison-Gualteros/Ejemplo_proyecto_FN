using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configuration
{
    public class Rol_PermisoConfiguration : IEntityTypeConfiguration<Rol_Permiso>
    {
        public void Configure(EntityTypeBuilder<Rol_Permiso> builder)
        {
            builder.ToTable("Rol_Permisos");

            builder.HasKey(rp => new { rp.Id_Rol, rp.Id_Permiso });

            builder.HasOne(rp => rp.Permiso)
                   .WithMany(p => p.Rol_Permisos)
                   .HasForeignKey(rp => rp.Id_Permiso)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rp => rp.Role)
                   .WithMany(r => r.Rol_Permisos) 
                   .HasForeignKey(rp => rp.Id_Rol)
                   .HasPrincipalKey(r => r.Id)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rp => new { rp.Id_Rol, rp.Id_Permiso })
                   .IsUnique();

            builder.HasData(
                // Administrador
                new Rol_Permiso { Id_Rol = "562419f5-eed1-473b-bcc1-9f2dbab182b4", Id_Permiso = 1 },
                new Rol_Permiso { Id_Rol = "562419f5-eed1-473b-bcc1-9f2dbab182b4", Id_Permiso = 2 },
                new Rol_Permiso { Id_Rol = "562419f5-eed1-473b-bcc1-9f2dbab182b4", Id_Permiso = 3 },
                new Rol_Permiso { Id_Rol = "562419f5-eed1-473b-bcc1-9f2dbab182b4", Id_Permiso = 4 },
                new Rol_Permiso { Id_Rol = "562419f5-eed1-473b-bcc1-9f2dbab182b4", Id_Permiso = 5 },

                // Cliente
                new Rol_Permiso { Id_Rol = "d12540b0-6de7-48dd-befa-066de9d3a6a0", Id_Permiso = 1 }
            );
        }
    }
}
