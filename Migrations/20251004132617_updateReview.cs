using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectTest1.Migrations
{
    /// <inheritdoc />
    public partial class updateReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellerReply",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SellerReplyAt",
                table: "Reviews",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerReply",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SellerReplyAt",
                table: "Reviews");
        }
    }
}
