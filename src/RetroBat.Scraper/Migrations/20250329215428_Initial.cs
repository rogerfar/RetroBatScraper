using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RetroBat.Scraper.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Platforms",
                columns: table => new
                {
                    PlatformId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScreenScraperId = table.Column<int>(type: "INTEGER", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Extensions = table.Column<string>(type: "TEXT", nullable: false),
                    Company = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    RomType = table.Column<string>(type: "TEXT", nullable: false),
                    MediaType = table.Column<string>(type: "TEXT", nullable: false),
                    Names = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platforms", x => x.PlatformId);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    SettingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.SettingId);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScreenScraperId = table.Column<int>(type: "INTEGER", nullable: true),
                    PlatformId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FileNameWithExtension = table.Column<string>(type: "TEXT", nullable: false),
                    FileNameWithoutExtension = table.Column<string>(type: "TEXT", nullable: false),
                    ScreenScraperData = table.Column<string>(type: "TEXT", nullable: true),
                    GameLinkData = table.Column<string>(type: "TEXT", nullable: true),
                    ScrapeStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ScrapeResult = table.Column<string>(type: "TEXT", nullable: true),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.GameId);
                    table.ForeignKey(
                        name: "FK_Games_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "PlatformId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "SettingId", "Key", "Type", "Value" },
                values: new object[,]
                {
                    { new Guid("12e63957-a757-4132-a495-bdce18b33e9b"), "ScreenScraperUserPassword", "String", "" },
                    { new Guid("34af0c52-c50a-4152-80bc-4023480a8c56"), "ScreenScraperDevId", "String", "" },
                    { new Guid("d09b3668-7b40-433f-aa97-f34e00b90170"), "ScreenScraperDevPassword", "String", "" },
                    { new Guid("d1b530ac-9b86-4138-bc8a-15f0b9443c60"), "RetroBatPath", "String", "" },
                    { new Guid("f15330ed-02f5-4de2-8e83-ddefd3a442f3"), "ScreenScraperUserName", "String", "" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_PlatformId",
                table: "Games",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Key",
                table: "Settings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Platforms");
        }
    }
}
