using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paga.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringGenerationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "last_generated_date",
                table: "incomes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "last_generated_date",
                table: "expenses",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_generated_date",
                table: "incomes");

            migrationBuilder.DropColumn(
                name: "last_generated_date",
                table: "expenses");
        }
    }
}
