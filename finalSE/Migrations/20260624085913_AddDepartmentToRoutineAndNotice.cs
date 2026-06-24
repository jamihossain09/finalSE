using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finalSE.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentToRoutineAndNotice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Routines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Notices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routines_DepartmentId",
                table: "Routines",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_DepartmentId",
                table: "Notices",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Departments_DepartmentId",
                table: "Notices",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Routines_Departments_DepartmentId",
                table: "Routines",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Departments_DepartmentId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Routines_Departments_DepartmentId",
                table: "Routines");

            migrationBuilder.DropIndex(
                name: "IX_Routines_DepartmentId",
                table: "Routines");

            migrationBuilder.DropIndex(
                name: "IX_Notices_DepartmentId",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Notices");
        }
    }
}
