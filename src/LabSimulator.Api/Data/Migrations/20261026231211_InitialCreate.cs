using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LabSimulator.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "labsim");

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "labsim",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scenarios",
                schema: "labsim",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scenarios", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                schema: "labsim",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matches", x => x.id);
                    table.ForeignKey(
                        name: "fk_matches_profiles_host_id",
                        column: x => x.host_id,
                        principalSchema: "labsim",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scenario_steps",
                schema: "labsim",
                columns: table => new
                {
                    scenario_code = table.Column<string>(type: "character varying(64)", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    stage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    max_points = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scenario_steps", x => new { x.scenario_code, x.code });
                    table.CheckConstraint("ck_scenario_steps_max_points", "max_points > 0");
                    table.ForeignKey(
                        name: "fk_scenario_steps_scenarios_scenario_code",
                        column: x => x.scenario_code,
                        principalSchema: "labsim",
                        principalTable: "scenarios",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_participants",
                schema: "labsim",
                columns: table => new
                {
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_match_participants", x => new { x.match_id, x.profile_id });
                    table.ForeignKey(
                        name: "fk_match_participants_matches_match_id",
                        column: x => x.match_id,
                        principalSchema: "labsim",
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_match_participants_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "labsim",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scenario_attempts",
                schema: "labsim",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    selected_medication = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    selected_pathology = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    remaining_seconds = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    max_score = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scenario_attempts", x => x.id);
                    table.CheckConstraint("ck_scenario_attempts_remaining_seconds", "remaining_seconds >= 0");
                    table.CheckConstraint("ck_scenario_attempts_score", "score >= 0 AND score <= max_score");
                    table.ForeignKey(
                        name: "fk_scenario_attempts_matches_match_id",
                        column: x => x.match_id,
                        principalSchema: "labsim",
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_scenario_attempts_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "labsim",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_scenario_attempts_scenarios_scenario_code",
                        column: x => x.scenario_code,
                        principalSchema: "labsim",
                        principalTable: "scenarios",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attempt_step_results",
                schema: "labsim",
                columns: table => new
                {
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    earned_points = table.Column<int>(type: "integer", nullable: false),
                    max_points = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attempt_step_results", x => new { x.attempt_id, x.step_code });
                    table.CheckConstraint("ck_attempt_step_results_points", "earned_points >= 0 AND earned_points <= max_points");
                    table.ForeignKey(
                        name: "fk_attempt_step_results_scenario_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalSchema: "labsim",
                        principalTable: "scenario_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "labsim",
                table: "scenarios",
                columns: new[] { "code", "is_active", "name" },
                values: new object[] { "infusion-pump", true, "Bomba de infusión" });

            migrationBuilder.InsertData(
                schema: "labsim",
                table: "scenario_steps",
                columns: new[] { "code", "scenario_code", "display_order", "max_points", "name", "stage" },
                values: new object[,]
                {
                    { "correct-area", "infusion-pump", 1, 100, "Área del hospital", "inspection" },
                    { "correct-medication", "infusion-pump", 10, 100, "Medicamento del paciente", "configuration" },
                    { "damaged-power-cable", "infusion-pump", 3, 100, "Cable de poder", "inspection" },
                    { "dirty-pump", "infusion-pump", 2, 100, "Bomba de infusión sucia", "inspection" },
                    { "infusion-set-installed", "infusion-pump", 7, 100, "Set de infusión", "configuration" },
                    { "line-configuration", "infusion-pump", 11, 100, "Configuración de las líneas en la bomba de infusión", "configuration" },
                    { "maintenance-sticker", "infusion-pump", 6, 100, "Sticker de mantenimiento", "inspection" },
                    { "patient-document", "infusion-pump", 9, 100, "Documento del paciente", "configuration" },
                    { "power-cable-disconnected", "infusion-pump", 4, 100, "Cable desconectado de la fuente de poder", "inspection" },
                    { "power-source-disconnected", "infusion-pump", 5, 100, "Fuente de poder desconectada", "inspection" },
                    { "purge", "infusion-pump", 8, 100, "Purga", "configuration" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_match_participants_profile_id",
                schema: "labsim",
                table: "match_participants",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_matches_host_id_started_at",
                schema: "labsim",
                table: "matches",
                columns: new[] { "host_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_scenario_attempts_match_id",
                schema: "labsim",
                table: "scenario_attempts",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "ix_scenario_attempts_profile_id_completed_at",
                schema: "labsim",
                table: "scenario_attempts",
                columns: new[] { "profile_id", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_scenario_attempts_scenario_code",
                schema: "labsim",
                table: "scenario_attempts",
                column: "scenario_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attempt_step_results",
                schema: "labsim");

            migrationBuilder.DropTable(
                name: "match_participants",
                schema: "labsim");

            migrationBuilder.DropTable(
                name: "scenario_steps",
                schema: "labsim");

            migrationBuilder.DropTable(
                name: "scenario_attempts",
                schema: "labsim");

            migrationBuilder.DropTable(
                name: "matches",
                schema: "labsim");

            migrationBuilder.DropTable(
                name: "scenarios",
                schema: "labsim");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "labsim");
        }
    }
}
