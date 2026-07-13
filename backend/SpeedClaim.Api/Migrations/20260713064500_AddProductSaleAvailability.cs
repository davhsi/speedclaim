using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpeedClaim.Api.Context;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    [DbContext(typeof(SpeedClaimDbContext))]
    [Migration("20260713064500_AddProductSaleAvailability")]
    public partial class AddProductSaleAvailability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_available_for_sale",
                table: "insurance_products",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_available_for_sale",
                table: "insurance_products");
        }
    }
}
