using Microsoft.EntityFrameworkCore.Migrations;

namespace Netptune.Entities.Migrations
{
    public partial class AddUniqueNextUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_WorkspaceId",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Projects",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(6)",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_WorkspaceId_Key",
                table: "Projects",
                columns: new[] { "WorkspaceId", "Key" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_WorkspaceId_Key",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Projects",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 6);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_WorkspaceId",
                table: "Projects",
                column: "WorkspaceId");
        }
    }
}
