using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Netptune.Entities.Migrations
{
    /// <inheritdoc />
    [Migration("20260504214000_AddPaginationIndexes")]
    public partial class AddPaginationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_workspace_occurred_id",
                table: "activity_logs",
                columns: new[] { "workspace_id", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_workspace_entity_occurred_id",
                table: "activity_logs",
                columns: new[] { "workspace_id", "entity_type", "entity_id", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_workspace_task_occurred_id",
                table: "activity_logs",
                columns: new[] { "workspace_id", "entity_type", "task_id", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_workspace_board_occurred_id",
                table: "activity_logs",
                columns: new[] { "workspace_id", "entity_type", "board_id", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_workspace_project_occurred_id",
                table: "activity_logs",
                columns: new[] { "workspace_id", "entity_type", "project_id", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_workspace_board_group_occurred_id",
                table: "activity_logs",
                columns: new[] { "workspace_id", "entity_type", "board_group_id", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_board_groups_board_deleted_sort_id",
                table: "board_groups",
                columns: new[] { "board_id", "is_deleted", "sort_order", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_comments_workspace_entity_created_id",
                table: "comments",
                columns: new[] { "workspace_id", "entity_type", "entity_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_project_task_app_users_task_user",
                table: "project_task_app_users",
                columns: new[] { "project_task_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_project_task_in_board_groups_group_sort_task",
                table: "project_task_in_board_groups",
                columns: new[] { "board_group_id", "sort_order", "project_task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_project_task_in_board_groups_task_group",
                table: "project_task_in_board_groups",
                columns: new[] { "project_task_id", "board_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_project_task_tags_task_tag",
                table: "project_task_tags",
                columns: new[] { "project_task_id", "tag_id" });

            migrationBuilder.CreateIndex(
                name: "ix_project_tasks_workspace_deleted_updated_id",
                table: "project_tasks",
                columns: new[] { "workspace_id", "is_deleted", "updated_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_project_tasks_workspace_project_deleted_updated_id",
                table: "project_tasks",
                columns: new[] { "workspace_id", "project_id", "is_deleted", "updated_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_project_tasks_workspace_sprint_deleted_updated_id",
                table: "project_tasks",
                columns: new[] { "workspace_id", "sprint_id", "is_deleted", "updated_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_project_tasks_workspace_status_deleted_updated_id",
                table: "project_tasks",
                columns: new[] { "workspace_id", "status", "is_deleted", "updated_at", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_project_tasks_workspace_status_deleted_updated_id", table: "project_tasks");
            migrationBuilder.DropIndex(name: "ix_project_tasks_workspace_sprint_deleted_updated_id", table: "project_tasks");
            migrationBuilder.DropIndex(name: "ix_project_tasks_workspace_project_deleted_updated_id", table: "project_tasks");
            migrationBuilder.DropIndex(name: "ix_project_tasks_workspace_deleted_updated_id", table: "project_tasks");
            migrationBuilder.DropIndex(name: "ix_project_task_tags_task_tag", table: "project_task_tags");
            migrationBuilder.DropIndex(name: "ix_project_task_in_board_groups_task_group", table: "project_task_in_board_groups");
            migrationBuilder.DropIndex(name: "ix_project_task_in_board_groups_group_sort_task", table: "project_task_in_board_groups");
            migrationBuilder.DropIndex(name: "ix_project_task_app_users_task_user", table: "project_task_app_users");
            migrationBuilder.DropIndex(name: "ix_comments_workspace_entity_created_id", table: "comments");
            migrationBuilder.DropIndex(name: "ix_board_groups_board_deleted_sort_id", table: "board_groups");
            migrationBuilder.DropIndex(name: "ix_activity_logs_workspace_board_group_occurred_id", table: "activity_logs");
            migrationBuilder.DropIndex(name: "ix_activity_logs_workspace_project_occurred_id", table: "activity_logs");
            migrationBuilder.DropIndex(name: "ix_activity_logs_workspace_board_occurred_id", table: "activity_logs");
            migrationBuilder.DropIndex(name: "ix_activity_logs_workspace_task_occurred_id", table: "activity_logs");
            migrationBuilder.DropIndex(name: "ix_activity_logs_workspace_entity_occurred_id", table: "activity_logs");
            migrationBuilder.DropIndex(name: "ix_activity_logs_workspace_occurred_id", table: "activity_logs");
        }
    }
}
