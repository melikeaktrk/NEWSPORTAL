using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRssTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RssFeedSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FeedUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastFetchedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RssFeedSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RssNewsItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OriginalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalPublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RssNewsItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RssNewsItems_RssFeedSources_FeedId",
                        column: x => x.FeedId,
                        principalTable: "RssFeedSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RssFeedSources_FeedUrl",
                table: "RssFeedSources",
                column: "FeedUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RssNewsItems_FeedId_ExternalId",
                table: "RssNewsItems",
                columns: new[] { "FeedId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RssNewsItems_Status_Category_OriginalPublishedAt",
                table: "RssNewsItems",
                columns: new[] { "Status", "Category", "OriginalPublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RssNewsItems");

            migrationBuilder.DropTable(
                name: "RssFeedSources");
        }
    }
}
