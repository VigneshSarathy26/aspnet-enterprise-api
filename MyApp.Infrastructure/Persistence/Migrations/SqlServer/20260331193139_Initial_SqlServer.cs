using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Infrastructure.Persistence.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Initial_SqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "billing_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    account_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    svc_provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    svc_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    svc_region = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    cost_amount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    cost_currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    usage_amount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    usage_currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    usage_unit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    period_start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    period_end = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    raw_payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    correlation_id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_br_account_id",
                table: "billing_records",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_br_account_period",
                table: "billing_records",
                columns: new[] { "account_id", "period_start", "period_end" });

            migrationBuilder.CreateIndex(
                name: "ix_br_correlation_id",
                table: "billing_records",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_br_period",
                table: "billing_records",
                columns: new[] { "period_start", "period_end" });

            migrationBuilder.CreateIndex(
                name: "ix_br_provider",
                table: "billing_records",
                column: "provider");

            migrationBuilder.CreateIndex(
                name: "ix_br_status",
                table: "billing_records",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_records");
        }
    }
}
