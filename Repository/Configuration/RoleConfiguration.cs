using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Repository.Configuration
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Role");

            builder.HasData(
                new Role
                {
                    Id = "562419f5-eed1-473b-bcc1-9f2dbab182b4",
                    Name = "Administrador",
                    NormalizedName = "ADMINISTRADOR"
                },
                new Role
                {
                    Id = "d12540b0-6de7-48dd-befa-066de9d3a6a0",
                    Name = "Cliente",
                    NormalizedName = "CLIENTE"
                }
            );
        }
    }
}
