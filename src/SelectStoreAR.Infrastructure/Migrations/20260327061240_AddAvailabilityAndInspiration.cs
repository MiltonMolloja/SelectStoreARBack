using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelectStoreAR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityAndInspiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "availability",
                table: "products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "inspiration",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "availability",
                table: "products");

            migrationBuilder.DropColumn(
                name: "inspiration",
                table: "products");
        }
    }
}
