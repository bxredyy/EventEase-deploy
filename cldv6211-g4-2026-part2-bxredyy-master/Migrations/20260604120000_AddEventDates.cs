using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventEase.Migrations
{
    public partial class AddEventDates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Unspecified));

            // Update the three seed events with proper dates
            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "EventId",
                keyValue: 1,
                columns: new[] { "StartDate", "EndDate" },
                values: new object[] {
                    new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Unspecified),
                    new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Unspecified)
                });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "EventId",
                keyValue: 2,
                columns: new[] { "StartDate", "EndDate" },
                values: new object[] {
                    new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Unspecified),
                    new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Unspecified)
                });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "EventId",
                keyValue: 3,
                columns: new[] { "StartDate", "EndDate" },
                values: new object[] {
                    new DateTime(2026, 12, 5, 0, 0, 0, DateTimeKind.Unspecified),
                    new DateTime(2026, 12, 5, 0, 0, 0, DateTimeKind.Unspecified)
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "StartDate", table: "Events");
            migrationBuilder.DropColumn(name: "EndDate", table: "Events");
        }
    }
}
