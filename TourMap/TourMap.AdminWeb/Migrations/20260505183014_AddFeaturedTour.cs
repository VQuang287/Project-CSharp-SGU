using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourMap.AdminWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddFeaturedTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeaturedImageUrl",
                table: "Tours",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Tours",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeaturedImageUrl",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Tours");
        }
    }
}
