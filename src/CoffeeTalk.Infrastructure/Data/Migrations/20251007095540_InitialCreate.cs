using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeTalk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coffee_bars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Theme = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DefaultMaxIngredientsPerHipster = table.Column<int>(type: "integer", nullable: false),
                    SubmissionPolicy = table.Column<int>(type: "integer", nullable: false),
                    SubmissionsLocked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coffee_bars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "brew_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoffeeBarId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brew_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_brew_sessions_coffee_bars_CoffeeBarId",
                        column: x => x.CoffeeBarId,
                        principalTable: "coffee_bars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hipsters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoffeeBarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NormalizedUsername = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaxIngredientQuota = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hipsters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hipsters_coffee_bars_CoffeeBarId",
                        column: x => x.CoffeeBarId,
                        principalTable: "coffee_bars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoffeeBarId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsConsumed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ingredients_coffee_bars_CoffeeBarId",
                        column: x => x.CoffeeBarId,
                        principalTable: "coffee_bars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "brew_cycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrewSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevealedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brew_cycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_brew_cycles_brew_sessions_BrewSessionId",
                        column: x => x.BrewSessionId,
                        principalTable: "brew_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_brew_cycles_ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoffeeBarId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    HipsterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submissions_coffee_bars_CoffeeBarId",
                        column: x => x.CoffeeBarId,
                        principalTable: "coffee_bars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_submissions_hipsters_HipsterId",
                        column: x => x.HipsterId,
                        principalTable: "hipsters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_submissions_ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrewCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterHipsterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetHipsterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CastAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_votes_brew_cycles_BrewCycleId",
                        column: x => x.BrewCycleId,
                        principalTable: "brew_cycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_votes_hipsters_VoterHipsterId",
                        column: x => x.VoterHipsterId,
                        principalTable: "hipsters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_brew_cycles_BrewSessionId",
                table: "brew_cycles",
                column: "BrewSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_brew_cycles_IngredientId",
                table: "brew_cycles",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_brew_sessions_CoffeeBarId",
                table: "brew_sessions",
                column: "CoffeeBarId");

            migrationBuilder.CreateIndex(
                name: "IX_coffee_bars_Code",
                table: "coffee_bars",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hipsters_CoffeeBarId_NormalizedUsername",
                table: "hipsters",
                columns: new[] { "CoffeeBarId", "NormalizedUsername" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ingredients_CoffeeBarId",
                table: "ingredients",
                column: "CoffeeBarId");

            migrationBuilder.CreateIndex(
                name: "IX_submissions_CoffeeBarId_IngredientId_HipsterId",
                table: "submissions",
                columns: new[] { "CoffeeBarId", "IngredientId", "HipsterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_submissions_HipsterId",
                table: "submissions",
                column: "HipsterId");

            migrationBuilder.CreateIndex(
                name: "IX_submissions_IngredientId",
                table: "submissions",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_votes_BrewCycleId_VoterHipsterId",
                table: "votes",
                columns: new[] { "BrewCycleId", "VoterHipsterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_votes_VoterHipsterId",
                table: "votes",
                column: "VoterHipsterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "submissions");

            migrationBuilder.DropTable(
                name: "votes");

            migrationBuilder.DropTable(
                name: "brew_cycles");

            migrationBuilder.DropTable(
                name: "hipsters");

            migrationBuilder.DropTable(
                name: "brew_sessions");

            migrationBuilder.DropTable(
                name: "ingredients");

            migrationBuilder.DropTable(
                name: "coffee_bars");
        }
    }
}
