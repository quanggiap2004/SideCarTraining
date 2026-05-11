using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideCar.Business.Migrations
{
    /// <inheritdoc />
    public partial class AddWarningSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WarningSentAt",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarningSentAt",
                table: "Users");
        }
    }
}
