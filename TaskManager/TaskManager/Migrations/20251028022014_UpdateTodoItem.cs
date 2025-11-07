using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTodoItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "TodoItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_ParentId",
                table: "TodoItems",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_TodoItems_ParentId",
                table: "TodoItems",
                column: "ParentId",
                principalTable: "TodoItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_TodoItems_ParentId",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_ParentId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "TodoItems");
        }
    }
}
