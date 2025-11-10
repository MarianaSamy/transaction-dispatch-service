using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransactionDispatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeleteAfterSend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeleteAfterSend",
                table: "DispatchJobs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleteAfterSend",
                table: "DispatchJobs");
        }
    }
}
