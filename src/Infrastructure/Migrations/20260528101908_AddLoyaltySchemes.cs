using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltySchemes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "loyalty_schemes",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    active = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false, defaultValue: "Y")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scheme_name = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scheme_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scheme_description = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scheme_tag = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    customer_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    area_scope = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    area_values = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    scheme_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Invoice")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    based_on = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    updated_by = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loyalty_schemes", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "loyalty_scheme_slabs",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    loyalty_scheme_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    tier_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value_from = table.Column<decimal>(type: "decimal(15,2)", precision: 15, scale: 2, nullable: false),
                    value_to = table.Column<decimal>(type: "decimal(15,2)", precision: 15, scale: 2, nullable: true),
                    reward_value = table.Column<decimal>(type: "decimal(15,2)", precision: 15, scale: 2, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loyalty_scheme_slabs", x => x.id);
                    table.ForeignKey(
                        name: "FK_loyalty_scheme_slabs_loyalty_schemes_loyalty_scheme_id",
                        column: x => x.loyalty_scheme_id,
                        principalTable: "loyalty_schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_scheme_slabs_loyalty_scheme_id",
                table: "loyalty_scheme_slabs",
                column: "loyalty_scheme_id");

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_schemes_scheme_code",
                table: "loyalty_schemes",
                column: "scheme_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_schemes_scheme_name",
                table: "loyalty_schemes",
                column: "scheme_name");

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_schemes_status",
                table: "loyalty_schemes",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loyalty_scheme_slabs");

            migrationBuilder.DropTable(
                name: "loyalty_schemes");
        }
    }
}
