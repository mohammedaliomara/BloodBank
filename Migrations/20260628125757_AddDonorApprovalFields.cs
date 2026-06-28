using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodBank.Migrations
{
    /// <inheritdoc />
    public partial class AddDonorApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullAddress",
                table: "Donors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationDate",
                table: "Donors",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Donors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Donors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Weight",
                table: "Donors",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BloodCenterId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BloodCenterId",
                table: "Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BloodCenterId",
                table: "Appointments",
                column: "BloodCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DonorId",
                table: "Appointments",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_BloodCenterId",
                table: "Accounts",
                column: "BloodCenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_BloodCenters_BloodCenterId",
                table: "Accounts",
                column: "BloodCenterId",
                principalTable: "BloodCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_BloodCenters_BloodCenterId",
                table: "Appointments",
                column: "BloodCenterId",
                principalTable: "BloodCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Donors_DonorId",
                table: "Appointments",
                column: "DonorId",
                principalTable: "Donors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_BloodCenters_BloodCenterId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_BloodCenters_BloodCenterId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Donors_DonorId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_BloodCenterId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DonorId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_BloodCenterId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "FullAddress",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "RegistrationDate",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "BloodCenterId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "BloodCenterId",
                table: "Accounts");
        }
    }
}
