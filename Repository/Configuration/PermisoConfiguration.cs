using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Configuration
{
    public class PermisoConfiguration : IEntityTypeConfiguration<Permiso>
    {
        public void Configure(EntityTypeBuilder<Permiso> builder)
        {
            builder.ToTable("Permisos");

            builder.HasKey(p => p.Id_Permiso);

            builder.Property(p => p.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(p => p.Nombre)
                .IsUnique();

            builder.HasData(
                new Permiso { Id_Permiso = 1, Nombre = "DOCUMENTOS_VER" },
                new Permiso { Id_Permiso = 2, Nombre = "DOCUMENTOS_CREAR" },
                new Permiso { Id_Permiso = 3, Nombre = "DOCUMENTOS_EDITAR" },
                new Permiso { Id_Permiso = 4, Nombre = "DOCUMENTOS_ELIMINAR" },
                new Permiso { Id_Permiso = 5, Nombre = "ROLES_ADMIN" }
            );
        }
    }
}
