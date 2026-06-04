using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260527092000_AddAttachmentToNewInvoices")]
public partial class AddAttachmentToNewInvoices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddColumnIfNotExists(migrationBuilder, "new_invoices", "attachment", "varchar(500) DEFAULT NULL AFTER `points`");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropColumnIfExists(migrationBuilder, "new_invoices", "attachment");
    }

    private static void AddColumnIfNotExists(MigrationBuilder migrationBuilder, string tableName, string columnName, string columnDefinition)
    {
        migrationBuilder.Sql($@"SET @column_exists = (
  SELECT COUNT(*)
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = '{EscapeSqlLiteral(tableName)}'
    AND COLUMN_NAME = '{EscapeSqlLiteral(columnName)}'
);");
        migrationBuilder.Sql($@"SET @migration_sql = IF(
  @column_exists = 0,
  'ALTER TABLE `{EscapeIdentifier(tableName)}` ADD COLUMN `{EscapeIdentifier(columnName)}` {EscapeSqlLiteral(columnDefinition)}',
  'SELECT 1'
);");
        migrationBuilder.Sql(@"PREPARE migration_stmt FROM @migration_sql;");
        migrationBuilder.Sql(@"EXECUTE migration_stmt;");
        migrationBuilder.Sql(@"DEALLOCATE PREPARE migration_stmt;");
    }

    private static void DropColumnIfExists(MigrationBuilder migrationBuilder, string tableName, string columnName)
    {
        migrationBuilder.Sql($@"SET @column_exists = (
  SELECT COUNT(*)
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = '{EscapeSqlLiteral(tableName)}'
    AND COLUMN_NAME = '{EscapeSqlLiteral(columnName)}'
);");
        migrationBuilder.Sql($@"SET @migration_sql = IF(
  @column_exists > 0,
  'ALTER TABLE `{EscapeIdentifier(tableName)}` DROP COLUMN `{EscapeIdentifier(columnName)}`',
  'SELECT 1'
);");
        migrationBuilder.Sql(@"PREPARE migration_stmt FROM @migration_sql;");
        migrationBuilder.Sql(@"EXECUTE migration_stmt;");
        migrationBuilder.Sql(@"DEALLOCATE PREPARE migration_stmt;");
    }

    private static string EscapeIdentifier(string value)
    {
        return value.Replace("`", "``", StringComparison.Ordinal);
    }

    private static string EscapeSqlLiteral(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
