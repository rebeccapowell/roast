using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeTalk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ingredients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "ingredients",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "ingredients");
        }
    }
}
