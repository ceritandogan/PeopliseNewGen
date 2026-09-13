using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Peoplise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialVideoInterview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseBotProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RetakesAllowed = table.Column<int>(type: "integer", nullable: false),
                    RetentionPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseBotProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseBotProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentFlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CaseFlows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CaseBotProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseFlows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseFlows_CaseBotProjects_CaseBotProjectId",
                        column: x => x.CaseBotProjectId,
                        principalTable: "CaseBotProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Competencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CaseBotProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Competencies_CaseBotProjects_CaseBotProjectId",
                        column: x => x.CaseBotProjectId,
                        principalTable: "CaseBotProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CaseBotProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportTemplates_CaseBotProjects_CaseBotProjectId",
                        column: x => x.CaseBotProjectId,
                        principalTable: "CaseBotProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseCodeReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<Guid>(type: "uuid", nullable: false),
                    Readability = table.Column<int>(type: "integer", nullable: false),
                    Functionality = table.Column<int>(type: "integer", nullable: false),
                    DataValidation = table.Column<int>(type: "integer", nullable: false),
                    UseCaseHandling = table.Column<int>(type: "integer", nullable: false),
                    Syntax = table.Column<int>(type: "integer", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCodeReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCodeReviews_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseReports_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseResults_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseScorings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StepId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    ScoredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseScorings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseScorings_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseStepConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextResponse = table.Column<string>(type: "text", nullable: true),
                    VideoUrl = table.Column<string>(type: "text", nullable: true),
                    DocumentUrl = table.Column<string>(type: "text", nullable: true),
                    TranscriptText = table.Column<string>(type: "text", nullable: true),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseStepConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseStepConversations_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseStepRetakeCounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<Guid>(type: "uuid", nullable: false),
                    RetakesUsed = table.Column<int>(type: "integer", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseStepRetakeCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseStepRetakeCounts_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseFlowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    PreparationTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    RecordingTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    RelatedCompetencyIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseFlowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseFlowSteps_CaseFlows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "CaseFlows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyBehavioralIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CompetencyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyBehavioralIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetencyBehavioralIndicators_Competencies_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "Competencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CompetencyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetencyLevels_Competencies_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "Competencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportTemplateSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ReportTemplateId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTemplateSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportTemplateSections_ReportTemplates_ReportTemplateId",
                        column: x => x.ReportTemplateId,
                        principalTable: "ReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseReportSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReportSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseReportSections_CaseReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "CaseReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseCompetencyResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    CaseResultId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCompetencyResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCompetencyResults_CaseResults_CaseResultId",
                        column: x => x.CaseResultId,
                        principalTable: "CaseResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseFlowStepRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseFlowStepRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseFlowStepRoutes_CaseFlowSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "CaseFlowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseBotProjects_TenantId",
                table: "CaseBotProjects",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCodeReviews_CaseId",
                table: "CaseCodeReviews",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCompetencyResults_CaseResultId",
                table: "CaseCompetencyResults",
                column: "CaseResultId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseFlows_CaseBotProjectId",
                table: "CaseFlows",
                column: "CaseBotProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseFlowStepRoutes_StepId",
                table: "CaseFlowStepRoutes",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseFlowSteps_FlowId",
                table: "CaseFlowSteps",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReports_CaseId",
                table: "CaseReports",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportSections_ReportId",
                table: "CaseReportSections",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseResults_CaseId",
                table: "CaseResults",
                column: "CaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_TenantId_CandidateId_CaseBotProjectId",
                table: "Cases",
                columns: new[] { "TenantId", "CandidateId", "CaseBotProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseScorings_CaseId",
                table: "CaseScorings",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseStepConversations_CaseId",
                table: "CaseStepConversations",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseStepRetakeCounts_CaseId",
                table: "CaseStepRetakeCounts",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Competencies_CaseBotProjectId",
                table: "Competencies",
                column: "CaseBotProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyBehavioralIndicators_CompetencyId",
                table: "CompetencyBehavioralIndicators",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyLevels_CompetencyId",
                table: "CompetencyLevels",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTemplates_CaseBotProjectId",
                table: "ReportTemplates",
                column: "CaseBotProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportTemplateSections_ReportTemplateId",
                table: "ReportTemplateSections",
                column: "ReportTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseCodeReviews");

            migrationBuilder.DropTable(
                name: "CaseCompetencyResults");

            migrationBuilder.DropTable(
                name: "CaseFlowStepRoutes");

            migrationBuilder.DropTable(
                name: "CaseReportSections");

            migrationBuilder.DropTable(
                name: "CaseScorings");

            migrationBuilder.DropTable(
                name: "CaseStepConversations");

            migrationBuilder.DropTable(
                name: "CaseStepRetakeCounts");

            migrationBuilder.DropTable(
                name: "CompetencyBehavioralIndicators");

            migrationBuilder.DropTable(
                name: "CompetencyLevels");

            migrationBuilder.DropTable(
                name: "ReportTemplateSections");

            migrationBuilder.DropTable(
                name: "CaseResults");

            migrationBuilder.DropTable(
                name: "CaseFlowSteps");

            migrationBuilder.DropTable(
                name: "CaseReports");

            migrationBuilder.DropTable(
                name: "Competencies");

            migrationBuilder.DropTable(
                name: "ReportTemplates");

            migrationBuilder.DropTable(
                name: "CaseFlows");

            migrationBuilder.DropTable(
                name: "Cases");

            migrationBuilder.DropTable(
                name: "CaseBotProjects");
        }
    }
}
