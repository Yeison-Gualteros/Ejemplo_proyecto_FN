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
    public class SolicitudConfigutation : IEntityTypeConfiguration<Solicitud>
    {
        public void Configure(EntityTypeBuilder<Solicitud> builder)
        {
            builder.HasData
            (
                new Solicitud
                {
                    Id_Solicitud = 1,
                    Id_Usuario = "1",
                    Tipo_Solicitud = "Acceso a Documentos",
                    Detalles = "Solicitud para obtener acceso a documentos confidenciales.",
                    Fecha_Solicitud = new DateTime(2025, 12, 1),
                    Estado = "Pendiente"
                }
            );
        }
    }
}
