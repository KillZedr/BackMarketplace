using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldInPayPalPaymentTransactionPaymentBasketId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "PaymentBasket",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string), 
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "PaymentBasket",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
