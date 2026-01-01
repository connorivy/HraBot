using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HraBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddImportance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "ImportanceToTakeCommand",
                table: "MessageFeedbacks",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImportanceToTakeCommand",
                table: "MessageFeedbacks");
        }
    }
}
