using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Peoplise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBotProjectRetentionPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetentionPeriodDays",
                table: "BotProjects",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetentionPeriodDays",
                table: "BotProjects");
        }
    }
}
