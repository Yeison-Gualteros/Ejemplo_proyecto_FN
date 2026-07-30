using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileNova.Migrations
{
    /// <inheritdoc />
    public partial class FechaRevisionDelDocumentov1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fecha_Revision",
                table: "Documentos");

            migrationBuilder.AddColumn<DateTime>(
                name: "Fecha_Revision",
                table: "DocumentoVersion",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fecha_Revision",
                table: "DocumentoVersion");

            migrationBuilder.AddColumn<DateTime>(
                name: "Fecha_Revision",
                table: "Documentos",
                type: "datetime2",
                nullable: true);
        }
    }
}
