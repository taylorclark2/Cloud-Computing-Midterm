using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloud_Computing_Midterm.Migrations
{
    /// <inheritdoc />
    public partial class RenameClassicToOld : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsClassic",
                table: "Shows",
                newName: "IsOld");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsOld",
                table: "Shows",
                newName: "IsClassic");
        }
    }
}
