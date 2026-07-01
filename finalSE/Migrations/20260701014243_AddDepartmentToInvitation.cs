using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finalSE.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentToInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Invitations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_DepartmentId",
                table: "Invitations",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Departments_DepartmentId",
                table: "Invitations",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Departments_DepartmentId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_DepartmentId",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Invitations");
        }
    }
}
