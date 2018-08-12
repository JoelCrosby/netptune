using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataPlane.Migrations
{
    public partial class addTasksFlags0 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Task",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Task",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "Task",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Task",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByUserId",
                table: "Task",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Task",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Task",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Task",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Flag",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Flag",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "Flag",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Flag",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByUserId",
                table: "Flag",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Flag",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Flag",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Flag",
                rowVersion: true,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Task");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Task");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Task");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Task");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "Task");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Task");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Task");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Task");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Flag");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Flag");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Flag");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Flag");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "Flag");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Flag");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Flag");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Flag");
        }
    }
}
