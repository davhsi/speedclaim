using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpeedClaim.Api.Context;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    [DbContext(typeof(SpeedClaimDbContext))]
    [Migration("20260713061500_AddMotorVehicleTypeToProducts")]
    public partial class AddMotorVehicleTypeToProducts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "motor_vehicle_type",
                table: "insurance_products",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "motor_vehicle_type",
                table: "insurance_products");
        }
    }
}
