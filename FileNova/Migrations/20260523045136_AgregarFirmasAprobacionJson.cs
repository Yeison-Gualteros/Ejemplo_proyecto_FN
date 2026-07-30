using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileNova.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFirmasAprobacionJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Antecedentes",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Conclusiones",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Falla",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Fecha1",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Fecha2",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "PlanAccion",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "RespuestaFabrica",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Resumen",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Documentos");

            migrationBuilder.RenameColumn(
                name: "Titulo",
                table: "Documentos",
                newName: "FirmasAprobacionJson");

            migrationBuilder.RenameColumn(
                name: "Solucion",
                table: "Documentos",
                newName: "ContenidoJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FirmasAprobacionJson",
                table: "Documentos",
                newName: "Titulo");

            migrationBuilder.RenameColumn(
                name: "ContenidoJson",
                table: "Documentos",
                newName: "Solucion");

            migrationBuilder.AddColumn<string>(
                name: "Antecedentes",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Conclusiones",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Falla",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Fecha1",
                table: "Documentos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Fecha2",
                table: "Documentos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanAccion",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RespuestaFabrica",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Resumen",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Revision",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
