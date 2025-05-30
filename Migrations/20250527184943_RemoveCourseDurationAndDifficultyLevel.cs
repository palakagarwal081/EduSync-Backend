using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduSync.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCourseDurationAndDifficultyLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseDuration",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "Courses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourseDuration",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DifficultyLevel",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
