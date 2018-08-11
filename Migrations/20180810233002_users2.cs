using Microsoft.EntityFrameworkCore.Migrations;

namespace DataPlane.Migrations
{
    public partial class users2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "ProjectTypes",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<string>(
                name: "DeletedByUserId",
                table: "ProjectTypes",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "ProjectTypes",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByUserId",
                table: "ProjectTypes",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "Projects",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<string>(
                name: "DeletedByUserId",
                table: "Projects",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "Projects",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByUserId",
                table: "Projects",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "ProjectTypes");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "Projects");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "ProjectTypes",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DeletedByUserId",
                table: "ProjectTypes",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByUserId",
                table: "ProjectTypes",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "Projects",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DeletedByUserId",
                table: "Projects",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByUserId",
                table: "Projects",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
