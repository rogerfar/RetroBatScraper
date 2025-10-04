using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RetroBat.Scraper.Migrations
{
    /// <inheritdoc />
    public partial class Initial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileNameWithExtension",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "Extensions",
                table: "Platforms",
                newName: "Extension");

            migrationBuilder.RenameColumn(
                name: "FileNameWithoutExtension",
                table: "Games",
                newName: "FileName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Extension",
                table: "Platforms",
                newName: "Extensions");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "Games",
                newName: "FileNameWithoutExtension");

            migrationBuilder.AddColumn<string>(
                name: "FileNameWithExtension",
                table: "Games",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
