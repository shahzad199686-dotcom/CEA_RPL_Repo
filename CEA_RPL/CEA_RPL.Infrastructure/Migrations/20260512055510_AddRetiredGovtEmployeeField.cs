using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CEA_RPL.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetiredGovtEmployeeField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRetiredGovtEmployee",
                table: "Declarations",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRetiredGovtEmployee",
                table: "Declarations");
        }
    }
}
