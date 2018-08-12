using Microsoft.EntityFrameworkCore.Migrations;

namespace DataPlane.Migrations
{
    public partial class fixnames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceAppUser_Workspace_WorkspaceId",
                table: "WorkspaceAppUser");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceProject_Workspace_WorkspaceId",
                table: "WorkspaceProject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Workspace",
                table: "Workspace");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Flag",
                table: "Flag");

            migrationBuilder.RenameTable(
                name: "Workspace",
                newName: "Workspaces");

            migrationBuilder.RenameTable(
                name: "Flag",
                newName: "Flags");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Workspaces",
                table: "Workspaces",
                column: "WorkspaceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Flags",
                table: "Flags",
                column: "FlagId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceAppUser_Workspaces_WorkspaceId",
                table: "WorkspaceAppUser",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceProject_Workspaces_WorkspaceId",
                table: "WorkspaceProject",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceAppUser_Workspaces_WorkspaceId",
                table: "WorkspaceAppUser");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceProject_Workspaces_WorkspaceId",
                table: "WorkspaceProject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Workspaces",
                table: "Workspaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Flags",
                table: "Flags");

            migrationBuilder.RenameTable(
                name: "Workspaces",
                newName: "Workspace");

            migrationBuilder.RenameTable(
                name: "Flags",
                newName: "Flag");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Workspace",
                table: "Workspace",
                column: "WorkspaceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Flag",
                table: "Flag",
                column: "FlagId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceAppUser_Workspace_WorkspaceId",
                table: "WorkspaceAppUser",
                column: "WorkspaceId",
                principalTable: "Workspace",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceProject_Workspace_WorkspaceId",
                table: "WorkspaceProject",
                column: "WorkspaceId",
                principalTable: "Workspace",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
