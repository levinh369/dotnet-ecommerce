using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectTest1.Migrations
{
    /// <inheritdoc />
    public partial class addMax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxPerUser",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxPerUser",
                table: "Vouchers");
        }
    }
}
