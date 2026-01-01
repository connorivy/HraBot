using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HraBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumMessages = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", rowVersion: true, nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageFeedbackItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShortDescription = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FeedbackItem = table.Column<byte>(type: "smallint", nullable: false),
                    FeedbackType = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageFeedbackItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Role = table.Column<byte>(type: "smallint", nullable: false),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    AiModel = table.Column<byte>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageFeedbacks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    AdditionalComments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageFeedbacks_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageFeedbackMessageFeedbackItem",
                columns: table => new
                {
                    MessageFeedbackItemsId = table.Column<long>(type: "bigint", nullable: false),
                    MessageFeedbacksId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageFeedbackMessageFeedbackItem", x => new { x.MessageFeedbackItemsId, x.MessageFeedbacksId });
                    table.ForeignKey(
                        name: "FK_MessageFeedbackMessageFeedbackItem_MessageFeedbackItems_Mes~",
                        column: x => x.MessageFeedbackItemsId,
                        principalTable: "MessageFeedbackItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageFeedbackMessageFeedbackItem_MessageFeedbacks_Message~",
                        column: x => x.MessageFeedbacksId,
                        principalTable: "MessageFeedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MessageFeedbackItems",
                columns: new[] { "Id", "FeedbackItem", "FeedbackType", "ShortDescription" },
                values: new object[,]
                {
                    { 1L, (byte)1, (byte)1, "no issues" },
                    { 2L, (byte)2, (byte)1, "no issues" },
                    { 3L, (byte)2, (byte)1, "no issues" },
                    { 4L, (byte)1, (byte)2, "incorrect" },
                    { 5L, (byte)1, (byte)2, "missing information" },
                    { 6L, (byte)1, (byte)2, "not applicable to question" },
                    { 7L, (byte)1, (byte)2, "not informed by citations" },
                    { 8L, (byte)1, (byte)2, "other" },
                    { 9L, (byte)2, (byte)2, "missing" },
                    { 10L, (byte)2, (byte)2, "incorrect" },
                    { 11L, (byte)2, (byte)2, "not applicable to question" },
                    { 12L, (byte)2, (byte)2, "other" },
                    { 13L, (byte)3, (byte)2, "too slow" },
                    { 14L, (byte)3, (byte)2, "other" },
                    { 15L, (byte)255, (byte)2, "other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageFeedbackMessageFeedbackItem_MessageFeedbacksId",
                table: "MessageFeedbackMessageFeedbackItem",
                column: "MessageFeedbacksId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageFeedbacks_MessageId",
                table: "MessageFeedbacks",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_Sequence",
                table: "Messages",
                columns: new[] { "ConversationId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageFeedbackMessageFeedbackItem");

            migrationBuilder.DropTable(
                name: "MessageFeedbackItems");

            migrationBuilder.DropTable(
                name: "MessageFeedbacks");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Conversations");
        }
    }
}
