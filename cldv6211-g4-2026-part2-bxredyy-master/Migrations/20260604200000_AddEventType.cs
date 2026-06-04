using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventEase.Migrations
{
    public partial class AddEventType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // POE Part 3A: create EventTypes table.
            migrationBuilder.CreateTable(
                name: "EventTypes",
                columns: table => new
                {
                    EventTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypes", x => x.EventTypeId);
                });

            // POE Part 3A: add EventTypeId column to Events.
            migrationBuilder.AddColumn<int>(
                name: "EventTypeId",
                table: "Events",
                type: "int",
                nullable: true);

            // POE Part 3A: seed the predefined event types.
            migrationBuilder.InsertData(
                table: "EventTypes",
                columns: new[] { "EventTypeId", "Name", "Description" },
                values: new object[,]
                {
                    { 1, "Conference",         "Professional industry conferences and summits" },
                    { 2, "Wedding",            "Wedding ceremonies and receptions" },
                    { 3, "Gala Dinner",        "Formal evening dinners and award ceremonies" },
                    { 4, "Corporate Meeting",  "Business meetings and team-building events" },
                    { 5, "Birthday & Private", "Birthday parties and private celebrations" },
                    { 6, "Exhibition",         "Exhibitions, trade shows and product launches" }
                });

            // POE Part 3A: assign types to existing events.
            migrationBuilder.UpdateData(table: "Events", keyColumn: "EventId", keyValue: 1, column: "EventTypeId", value: 3);
            migrationBuilder.UpdateData(table: "Events", keyColumn: "EventId", keyValue: 2, column: "EventTypeId", value: 1);
            migrationBuilder.UpdateData(table: "Events", keyColumn: "EventId", keyValue: 3, column: "EventTypeId", value: 2);

            // POE Part 3A: index and FK constraint.
            migrationBuilder.CreateIndex(
                name: "IX_Events_EventTypeId",
                table: "Events",
                column: "EventTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_EventTypes_EventTypeId",
                table: "Events",
                column: "EventTypeId",
                principalTable: "EventTypes",
                principalColumn: "EventTypeId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Events_EventTypes_EventTypeId", table: "Events");
            migrationBuilder.DropIndex(name: "IX_Events_EventTypeId", table: "Events");
            migrationBuilder.DropColumn(name: "EventTypeId", table: "Events");
            migrationBuilder.DropTable(name: "EventTypes");
        }
    }
}
