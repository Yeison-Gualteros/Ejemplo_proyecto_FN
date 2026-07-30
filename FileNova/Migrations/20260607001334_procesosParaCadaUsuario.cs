using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileNova.Migrations
{
    /// <inheritdoc />
    public partial class procesosParaCadaUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdProceso",
                table: "User",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "1",
                column: "IdProceso",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_User_IdProceso",
                table: "User",
                column: "IdProceso");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Procesos_IdProceso",
                table: "User",
                column: "IdProceso",
                principalTable: "Procesos",
                principalColumn: "IdProceso");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Procesos_IdProceso",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_IdProceso",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IdProceso",
                table: "User");
        }
    }
}
