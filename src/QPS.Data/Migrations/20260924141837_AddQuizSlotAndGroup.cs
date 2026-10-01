using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizSlotAndGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "Quizzes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SlotNumber",
                table: "Quizzes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Group",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "SlotNumber",
                table: "Quizzes");
        }
    }
}
