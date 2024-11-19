using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Application.Migrations
{
    /// <inheritdoc />
<<<<<<<< HEAD:Payment.Application/Migrations/20241118084827_PymentMetodInStripeDonation.cs
    public partial class PymentMetodInStripeDonation : Migration
========
    public partial class UserAddEntitiGuidMigration : Migration
>>>>>>>> 85c9961958ea166047f3a5ec339b5bce087ed752:Payment.Application/Migrations/20241018112837_UserAddEntitiGuidMigration.cs
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
<<<<<<<< HEAD:Payment.Application/Migrations/20241118084827_PymentMetodInStripeDonation.cs
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "StripeDonation",
                type: "text",
                nullable: true);
========
            migrationBuilder.RenameColumn(
                name: "PhoneNamber",
                table: "User",
                newName: "PhoneNumber");
>>>>>>>> 85c9961958ea166047f3a5ec339b5bce087ed752:Payment.Application/Migrations/20241018112837_UserAddEntitiGuidMigration.cs
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) 
        {
<<<<<<<< HEAD:Payment.Application/Migrations/20241118084827_PymentMetodInStripeDonation.cs
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "StripeDonation");
========
            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "User",
                newName: "PhoneNamber");
>>>>>>>> 85c9961958ea166047f3a5ec339b5bce087ed752:Payment.Application/Migrations/20241018112837_UserAddEntitiGuidMigration.cs
        }
    }
}
