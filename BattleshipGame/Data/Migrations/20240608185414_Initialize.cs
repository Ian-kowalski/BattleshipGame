using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BattleshipGame.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initialize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Cells",
                table: "GameStates",
                newName: "TrackingBoard");

            migrationBuilder.AddColumn<string>(
                name: "PlayerBoard",
                table: "GameStates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerBoard",
                table: "GameStates");

            migrationBuilder.RenameColumn(
                name: "TrackingBoard",
                table: "GameStates",
                newName: "Cells");
        }
    }
}
