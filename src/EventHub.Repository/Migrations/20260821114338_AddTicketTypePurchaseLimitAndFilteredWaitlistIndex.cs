using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTypePurchaseLimitAndFilteredWaitlistIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Waitlists_EventId_TicketTypeId_AttendeeId",
                table: "Waitlists");

            migrationBuilder.AddColumn<int>(
                name: "MaxTicketsPerUser",
                table: "TicketTypes",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.CreateIndex(
                name: "IX_Waitlists_EventId_TicketTypeId_AttendeeId",
                table: "Waitlists",
                columns: new[] { "EventId", "TicketTypeId", "AttendeeId" },
                unique: true,
                filter: "[Status] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Waitlists_EventId_TicketTypeId_AttendeeId",
                table: "Waitlists");

            migrationBuilder.DropColumn(
                name: "MaxTicketsPerUser",
                table: "TicketTypes");

            migrationBuilder.CreateIndex(
                name: "IX_Waitlists_EventId_TicketTypeId_AttendeeId",
                table: "Waitlists",
                columns: new[] { "EventId", "TicketTypeId", "AttendeeId" },
                unique: true);
        }
    }
}
