using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaPlayeras.Web.Migrations
{
    /// <inheritdoc />
    public partial class ProductMainImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MainImagePath",
                table: "Products",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainImagePath",
                table: "Products");
        }
    }
}
