using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Peoplise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialHrBot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BotProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Interface = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotFlows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    BotProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotFlows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotFlows_BotProjects_BotProjectId",
                        column: x => x.BotProjectId,
                        principalTable: "BotProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BotKnowledgebases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BotProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotKnowledgebases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotKnowledgebases_BotProjects_BotProjectId",
                        column: x => x.BotProjectId,
                        principalTable: "BotProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BotProjectVariables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BotProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotProjectVariables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotProjectVariables_BotProjects_BotProjectId",
                        column: x => x.BotProjectId,
                        principalTable: "BotProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateResponse = table.Column<string>(type: "text", nullable: true),
                    LoggedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationLogs_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationVariables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationVariables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationVariables_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BotFlowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    QuickReplyOptions = table.Column<string[]>(type: "text[]", nullable: false),
                    CaptureVariableKey = table.Column<string>(type: "text", nullable: true),
                    IsFinalStep = table.Column<bool>(type: "boolean", nullable: false),
                    IsScreenOut = table.Column<bool>(type: "boolean", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotFlowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotFlowSteps_BotFlows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "BotFlows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BotKnowledgebaseQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    Keywords = table.Column<string[]>(type: "text[]", nullable: false),
                    KnowledgebaseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotKnowledgebaseQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotKnowledgebaseQuestions_BotKnowledgebases_KnowledgebaseId",
                        column: x => x.KnowledgebaseId,
                        principalTable: "BotKnowledgebases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BotFlowStepRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConditionType = table.Column<int>(type: "integer", nullable: false),
                    Keywords = table.Column<string[]>(type: "text[]", nullable: false),
                    RouteType = table.Column<int>(type: "integer", nullable: false),
                    TargetFlowId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    StepId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotFlowStepRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotFlowStepRoutes_BotFlowSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "BotFlowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BotKnowledgebaseAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    KnowledgebaseQuestionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotKnowledgebaseAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotKnowledgebaseAnswers_BotKnowledgebaseQuestions_Knowledge~",
                        column: x => x.KnowledgebaseQuestionId,
                        principalTable: "BotKnowledgebaseQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BotFlows_BotProjectId",
                table: "BotFlows",
                column: "BotProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BotFlowStepRoutes_StepId",
                table: "BotFlowStepRoutes",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_BotFlowSteps_FlowId",
                table: "BotFlowSteps",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_BotKnowledgebaseAnswers_KnowledgebaseQuestionId",
                table: "BotKnowledgebaseAnswers",
                column: "KnowledgebaseQuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BotKnowledgebaseQuestions_KnowledgebaseId",
                table: "BotKnowledgebaseQuestions",
                column: "KnowledgebaseId");

            migrationBuilder.CreateIndex(
                name: "IX_BotKnowledgebases_BotProjectId",
                table: "BotKnowledgebases",
                column: "BotProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BotProjects_TenantId",
                table: "BotProjects",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BotProjectVariables_BotProjectId",
                table: "BotProjectVariables",
                column: "BotProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationLogs_ConversationId",
                table: "ConversationLogs",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TenantId_CandidateId_BotProjectId",
                table: "Conversations",
                columns: new[] { "TenantId", "CandidateId", "BotProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationVariables_ConversationId",
                table: "ConversationVariables",
                column: "ConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotFlowStepRoutes");

            migrationBuilder.DropTable(
                name: "BotKnowledgebaseAnswers");

            migrationBuilder.DropTable(
                name: "BotProjectVariables");

            migrationBuilder.DropTable(
                name: "ConversationLogs");

            migrationBuilder.DropTable(
                name: "ConversationVariables");

            migrationBuilder.DropTable(
                name: "BotFlowSteps");

            migrationBuilder.DropTable(
                name: "BotKnowledgebaseQuestions");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "BotFlows");

            migrationBuilder.DropTable(
                name: "BotKnowledgebases");

            migrationBuilder.DropTable(
                name: "BotProjects");
        }
    }
}
