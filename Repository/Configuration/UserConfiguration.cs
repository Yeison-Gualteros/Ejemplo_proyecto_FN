using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Repository.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
           

            builder.HasData(
                new User
                {
                    Id = "1", 
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@test.com",
                    NormalizedEmail = "ADMIN@TEST.COM",
                    EmailConfirmed = true,
                    PasswordHash = "AQAAAAIAAYagAAAAEBGZiSMC2XDHccsUuRCJdG0VuDXu6I7CTGDK4JVO3oX11hZ+dOcdc1TsntsHaPwjAQ==", // contraseña fija Admin123!
                    SecurityStamp = "11111111-1111-1111-1111-111111111111",
                    ConcurrencyStamp = "22222222-2222-2222-2222-222222222222",
                    Nombre = "Admin",
                    Apellido = "Sistema",
                    Estado = 1,
                    FechaCreacion = new DateTime(2025, 12, 16),
                    
                }
            );
        }
    }
}
