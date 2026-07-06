using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260612090000_DropSeparateDistributorRetailerTables")]
    public partial class DropSeparateDistributorRetailerTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET FOREIGN_KEY_CHECKS=0");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `secondary_customers`");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `master_distributors`");
            migrationBuilder.Sql("SET FOREIGN_KEY_CHECKS=1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
