using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CEA_RPL.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeclarationCheckboxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Declaration1Accepted",
                table: "Declarations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Declaration2Accepted",
                table: "Declarations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Declaration1Accepted",
                table: "Declarations");

            migrationBuilder.DropColumn(
                name: "Declaration2Accepted",
                table: "Declarations");
        }
    }
}
