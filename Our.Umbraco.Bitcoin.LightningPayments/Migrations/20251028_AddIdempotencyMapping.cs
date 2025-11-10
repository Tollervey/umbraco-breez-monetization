using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Our.Umbraco.Bitcoin.LightningPayments.Migrations
{
 public partial class AddIdempotencyMapping : Migration
 {
 protected override void Up(MigrationBuilder migrationBuilder)
 {
 migrationBuilder.CreateTable(
 name: "IdempotencyMappings",
 columns: table => new
 {
 IdempotencyKey = table.Column<string>(type: "TEXT", nullable: false),
 PaymentHash = table.Column<string>(type: "TEXT", nullable: false),
 Invoice = table.Column<string>(type: "TEXT", nullable: false),
 CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
 Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue:0)
 },
 constraints: table =>
 {
 table.PrimaryKey("PK_IdempotencyMappings", x => x.IdempotencyKey);
 });
 }

 protected override void Down(MigrationBuilder migrationBuilder)
 {
 migrationBuilder.DropTable(
 name: "IdempotencyMappings");
 }
 }
}

