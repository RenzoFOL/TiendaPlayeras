using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaPlayeras.Web.Migrations
{
    /// <inheritdoc />
    public partial class ProductVariantOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedColorsCsv",
                table: "Products",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedFitsCsv",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedSizesCsv",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseColor",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseFit",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseSize",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedColorsCsv",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AllowedFitsCsv",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AllowedSizesCsv",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UseColor",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UseFit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UseSize",
                table: "Products");
        }
    }
}
