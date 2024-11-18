using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Application.Migrations
{
    /// <inheritdoc />
    public partial class PymentMetodInStripeDonation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "StripeDonation",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "StripeDonation");
        }
    }
}
