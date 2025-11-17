using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaPlayeras.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantitySizeToOrderTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "OrderTickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "OrderTickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "OrderTickets",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "OrderTickets");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "OrderTickets");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "OrderTickets");
        }
    }
}
