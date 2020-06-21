using Microsoft.EntityFrameworkCore.Migrations;

namespace Netptune.Entities.Migrations
{
    public partial class UniqueNextUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_ProjectId",
                table: "ProjectTasks");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Projects",
                maxLength: 6,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId_ProjectScopeId",
                table: "ProjectTasks",
                columns: new[] { "ProjectId", "ProjectScopeId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_ProjectId_ProjectScopeId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Projects");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId",
                table: "ProjectTasks",
                column: "ProjectId");
        }
    }
}
