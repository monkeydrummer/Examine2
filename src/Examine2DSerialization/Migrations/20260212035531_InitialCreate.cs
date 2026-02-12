using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Examine2DSerialization.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Units = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Boundaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsClosed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    BoundaryType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boundaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Boundaries_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MaterialType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    YoungModulus = table.Column<double>(type: "REAL", nullable: true),
                    PoissonRatio = table.Column<double>(type: "REAL", nullable: true),
                    Density = table.Column<double>(type: "REAL", nullable: true),
                    E1 = table.Column<double>(type: "REAL", nullable: true),
                    E2 = table.Column<double>(type: "REAL", nullable: true),
                    Nu12 = table.Column<double>(type: "REAL", nullable: true),
                    Nu23 = table.Column<double>(type: "REAL", nullable: true),
                    G12 = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialProperties_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Queries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    QueryType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Queries_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrengthCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CriterionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Cohesion = table.Column<double>(type: "REAL", nullable: true),
                    FrictionAngle = table.Column<double>(type: "REAL", nullable: true),
                    Mb = table.Column<double>(type: "REAL", nullable: true),
                    S = table.Column<double>(type: "REAL", nullable: true),
                    A = table.Column<double>(type: "REAL", nullable: true),
                    Sci = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrengthCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrengthCriteria_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StressGrids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MinX = table.Column<double>(type: "REAL", nullable: false),
                    MinY = table.Column<double>(type: "REAL", nullable: false),
                    MaxX = table.Column<double>(type: "REAL", nullable: false),
                    MaxY = table.Column<double>(type: "REAL", nullable: false),
                    XPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    YPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StressGrids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StressGrids_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vertices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BoundaryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<double>(type: "REAL", nullable: false),
                    Y = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vertices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vertices_Boundaries_BoundaryId",
                        column: x => x.BoundaryId,
                        principalTable: "Boundaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueryPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QueryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<double>(type: "REAL", nullable: false),
                    Y = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryPoints_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boundaries_ProjectId",
                table: "Boundaries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialProperties_ProjectId",
                table: "MaterialProperties",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Queries_ProjectId",
                table: "Queries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryPoints_QueryId_Order",
                table: "QueryPoints",
                columns: new[] { "QueryId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_StrengthCriteria_ProjectId",
                table: "StrengthCriteria",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StressGrids_ProjectId",
                table: "StressGrids",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Vertices_BoundaryId_Order",
                table: "Vertices",
                columns: new[] { "BoundaryId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialProperties");

            migrationBuilder.DropTable(
                name: "QueryPoints");

            migrationBuilder.DropTable(
                name: "StrengthCriteria");

            migrationBuilder.DropTable(
                name: "StressGrids");

            migrationBuilder.DropTable(
                name: "Vertices");

            migrationBuilder.DropTable(
                name: "Queries");

            migrationBuilder.DropTable(
                name: "Boundaries");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
