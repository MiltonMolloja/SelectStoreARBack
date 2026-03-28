using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelectStoreAR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingChangesAndPriceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "availability",
                table: "products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Unknown");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_synced_at",
                table: "products",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_telegram_raw",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "price_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    price_usd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    changed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_price_history_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_price_history_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_pending_changes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    TelegramSyncBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    telegram_msg_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    change_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    raw_telegram_text = table.Column<string>(type: "text", nullable: false),
                    proposed_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    proposed_brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    proposed_description = table.Column<string>(type: "text", nullable: false),
                    proposed_price_usd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    proposed_availability = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    proposed_inspiration = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    proposed_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    current_price_usd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    review_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_pending_changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_pending_changes_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_price_history_product",
                table: "price_history",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_price_history_OrderId",
                table: "price_history",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "idx_pending_batch",
                table: "product_pending_changes",
                column: "TelegramSyncBatchId");

            migrationBuilder.CreateIndex(
                name: "idx_pending_product",
                table: "product_pending_changes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "idx_pending_status",
                table: "product_pending_changes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_history");

            migrationBuilder.DropTable(
                name: "product_pending_changes");

            migrationBuilder.DropColumn(
                name: "last_synced_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "last_telegram_raw",
                table: "products");

            migrationBuilder.AlterColumn<string>(
                name: "availability",
                table: "products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
