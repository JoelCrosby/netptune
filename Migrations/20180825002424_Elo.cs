using Microsoft.EntityFrameworkCore.Migrations;

namespace DataPlane.Migrations
{
    public partial class Elo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectUser_Projects_ProjectId",
                table: "ProjectUser");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectUser_AspNetUsers_UserId",
                table: "ProjectUser");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceAppUser_Projects_ProjectId",
                table: "WorkspaceAppUser");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceAppUser_AspNetUsers_UserId",
                table: "WorkspaceAppUser");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceAppUser_Workspaces_WorkspaceId",
                table: "WorkspaceAppUser");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceProject_AspNetUsers_AppUserId",
                table: "WorkspaceProject");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceProject_Projects_ProjectId",
                table: "WorkspaceProject");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceProject_Workspaces_WorkspaceId",
                table: "WorkspaceProject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkspaceProject",
                table: "WorkspaceProject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkspaceAppUser",
                table: "WorkspaceAppUser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectUser",
                table: "ProjectUser");

            migrationBuilder.RenameTable(
                name: "WorkspaceProject",
                newName: "WorkspaceProjects");

            migrationBuilder.RenameTable(
                name: "WorkspaceAppUser",
                newName: "WorkspaceAppUsers");

            migrationBuilder.RenameTable(
                name: "ProjectUser",
                newName: "ProjectUsers");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceProject_WorkspaceId",
                table: "WorkspaceProjects",
                newName: "IX_WorkspaceProjects_WorkspaceId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceProject_ProjectId",
                table: "WorkspaceProjects",
                newName: "IX_WorkspaceProjects_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceProject_AppUserId",
                table: "WorkspaceProjects",
                newName: "IX_WorkspaceProjects_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceAppUser_WorkspaceId",
                table: "WorkspaceAppUsers",
                newName: "IX_WorkspaceAppUsers_WorkspaceId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceAppUser_UserId",
                table: "WorkspaceAppUsers",
                newName: "IX_WorkspaceAppUsers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceAppUser_ProjectId",
                table: "WorkspaceAppUsers",
                newName: "IX_WorkspaceAppUsers_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectUser_UserId",
                table: "ProjectUsers",
                newName: "IX_ProjectUsers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectUser_ProjectId",
                table: "ProjectUsers",
                newName: "IX_ProjectUsers_ProjectId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkspaceProjects",
                table: "WorkspaceProjects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkspaceAppUsers",
                table: "WorkspaceAppUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectUsers",
                table: "ProjectUsers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUsers_Projects_ProjectId",
                table: "ProjectUsers",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUsers_AspNetUsers_UserId",
                table: "ProjectUsers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceAppUsers_Projects_ProjectId",
                table: "WorkspaceAppUsers",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceAppUsers_AspNetUsers_UserId",
                table: "WorkspaceAppUsers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceAppUsers_Workspaces_WorkspaceId",
                table: "WorkspaceAppUsers",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceProjects_AspNetUsers_AppUserId",
                table: "WorkspaceProjects",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceProjects_Projects_ProjectId",
                table: "WorkspaceProjects",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceProjects_Workspaces_WorkspaceId",
                table: "WorkspaceProjects",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectUsers_Projects_ProjectId",
                table: "ProjectUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectUsers_AspNetUsers_UserId",
                table: "ProjectUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceAppUsers_Projects_ProjectId",
                table: "WorkspaceAppUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceAppUsers_AspNetUsers_UserId",
                table: "WorkspaceAppUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceAppUsers_Workspaces_WorkspaceId",
                table: "WorkspaceAppUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceProjects_AspNetUsers_AppUserId",
                table: "WorkspaceProjects");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceProjects_Projects_ProjectId",
                table: "WorkspaceProjects");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceProjects_Workspaces_WorkspaceId",
                table: "WorkspaceProjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkspaceProjects",
                table: "WorkspaceProjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkspaceAppUsers",
                table: "WorkspaceAppUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectUsers",
                table: "ProjectUsers");

            migrationBuilder.RenameTable(
                name: "WorkspaceProjects",
                newName: "WorkspaceProject");

            migrationBuilder.RenameTable(
                name: "WorkspaceAppUsers",
                newName: "WorkspaceAppUser");

            migrationBuilder.RenameTable(
                name: "ProjectUsers",
                newName: "ProjectUser");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceProjects_WorkspaceId",
                table: "WorkspaceProject",
                newName: "IX_WorkspaceProject_WorkspaceId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceProjects_ProjectId",
                table: "WorkspaceProject",
                newName: "IX_WorkspaceProject_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceProjects_AppUserId",
                table: "WorkspaceProject",
                newName: "IX_WorkspaceProject_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceAppUsers_WorkspaceId",
                table: "WorkspaceAppUser",
                newName: "IX_WorkspaceAppUser_WorkspaceId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceAppUsers_UserId",
                table: "WorkspaceAppUser",
                newName: "IX_WorkspaceAppUser_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkspaceAppUsers_ProjectId",
                table: "WorkspaceAppUser",
                newName: "IX_WorkspaceAppUser_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectUsers_UserId",
                table: "ProjectUser",
                newName: "IX_ProjectUser_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectUsers_ProjectId",
                table: "ProjectUser",
                newName: "IX_ProjectUser_ProjectId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkspaceProject",
                table: "WorkspaceProject",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkspaceAppUser",
                table: "WorkspaceAppUser",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectUser",
                table: "ProjectUser",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUser_Projects_ProjectId",
                table: "ProjectUser",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUser_AspNetUsers_UserId",
                table: "ProjectUser",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceAppUser_Projects_ProjectId",
                table: "WorkspaceAppUser",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceAppUser_AspNetUsers_UserId",
                table: "WorkspaceAppUser",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceAppUser_Workspaces_WorkspaceId",
                table: "WorkspaceAppUser",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceProject_AspNetUsers_AppUserId",
                table: "WorkspaceProject",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceProject_Projects_ProjectId",
                table: "WorkspaceProject",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceProject_Workspaces_WorkspaceId",
                table: "WorkspaceProject",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
