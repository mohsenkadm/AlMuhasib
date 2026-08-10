using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CloudAccountantProFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstallmentId",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReconciled",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciledAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReconciledBy",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RelatedInvoiceId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedThroughDate",
                table: "BusinessSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PeriodLockEnabled",
                table: "BusinessSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_InstallmentId",
                table: "Vouchers",
                column: "InstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_InvoiceId",
                table: "Vouchers",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_RelatedInvoiceId",
                table: "Invoices",
                column: "RelatedInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Invoices_RelatedInvoiceId",
                table: "Invoices",
                column: "RelatedInvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Installments_InstallmentId",
                table: "Vouchers",
                column: "InstallmentId",
                principalTable: "Installments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Invoices_InvoiceId",
                table: "Vouchers",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Invoices_RelatedInvoiceId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Installments_InstallmentId",
                table: "Vouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Invoices_InvoiceId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_InstallmentId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_InvoiceId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_RelatedInvoiceId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InstallmentId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "IsReconciled",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ReconciledAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ReconciledBy",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "RelatedInvoiceId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LockedThroughDate",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "PeriodLockEnabled",
                table: "BusinessSettings");
        }
    }
}
