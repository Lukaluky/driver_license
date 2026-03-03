using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrderService.Infrastructure.Data;

#nullable disable

namespace OrderService.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260220103000_AddUserIin")]
    public partial class AddUserIin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Iin",
                table: "Users",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Iin",
                table: "Users",
                column: "Iin",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Iin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Iin",
                table: "Users");
        }
    }
}
