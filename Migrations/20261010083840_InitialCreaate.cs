using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudentApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreaate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Installments",
                columns: table => new
                {
                    StudentNo = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    InstallmentNo = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "NVARCHAR2(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    PaidDate = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installments", x => new { x.StudentNo, x.InstallmentNo });
                });

            migrationBuilder.InsertData(
                table: "Installments",
                columns: new[] { "InstallmentNo", "StudentNo", "Amount", "Currency", "PaidDate", "Status" },
                values: new object[,]
                {
                    { 1, "STU123", 2500.00m, "ZAR", new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Paid" },
                    { 2, "STU123", 2500.00m, "ZAR", null, "Pending" },
                    { 3, "STU123", 2750.00m, "ZAR", null, "Pending" },
                    { 4, "STU123", 2750.00m, "ZAR", null, "Pending" },
                    { 1, "STU456", 1800.00m, "ZAR", null, "Pending" },
                    { 2, "STU456", 1800.00m, "ZAR", null, "Pending" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Installments");
        }
    }
}
