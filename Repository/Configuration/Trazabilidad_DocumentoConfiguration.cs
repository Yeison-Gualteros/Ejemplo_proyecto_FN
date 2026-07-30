using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Configuration
{
    internal class Trazabilidad_DocumentoConfiguration : IEntityTypeConfiguration<Trazabilidad_Documento>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Trazabilidad_Documento> builder)
        {
            
        }
    }
}
