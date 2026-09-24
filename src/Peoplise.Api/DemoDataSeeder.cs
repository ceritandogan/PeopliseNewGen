using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.SharedKernel.MultiTenancy;
using VideoStepType = Peoplise.Modules.VideoInterview.Domain.ValueObjects.StepType;

namespace Peoplise.Api;

/// <summary>
/// Development-only seed data, same spirit as <c>DatabaseSeeder</c> but living here
/// instead: it needs the ATS and HrBot domain types directly (<c>Position</c>,
/// <c>BotProject</c>, ...), and <c>Peoplise.Infrastructure</c> deliberately has no
/// reference to any module project (see its own csproj) — only <c>Peoplise.Api</c>
/// references every module. Seeds one demo Position with a working default HrBot flow,
/// so there's something real for the candidate app's apply → chat journey to run
/// against before any real Flow-authoring UI exists.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        var hasBotProject = await context.Set<BotProject>().IgnoreQueryFilters().AnyAsync(cancellationToken);
        var hasCaseBotProject = await context.Set<CaseBotProject>().IgnoreQueryFilters().AnyAsync(cancellationToken);

        if (hasBotProject && hasCaseBotProject) return;

        if (hasBotProject != hasCaseBotProject)
        {
            // Both aggregates are seeded together below in one SaveChangesAsync, so this
            // can only happen against a local database seeded by an older version of
            // this method that predates CaseBotProject seeding — gating on BotProject
            // alone (the original check) would silently skip CaseBotProject/CaseFlows
            // forever on such a database. Reseeding here instead of bailing would insert
            // a second demo Position rather than repairing the gap, so fail loudly.
            throw new InvalidOperationException(
                "Demo data is partially seeded (BotProject and CaseBotProject existence disagree) — " +
                "reset the local Postgres volume (docker compose down -v) and restart.");
        }

        using var _ = AmbientTenantOverride.Begin(TenantId.From(DatabaseSeeder.DemoTenantId));

        var position = Position.Create(
            "HR Bot Demo — Backend Engineer", "Engineering", "Istanbul", "Turkey",
            WorkMode.Hybrid, SeniorityLevel.Mid, EmploymentType.FullTime);

        var workflow = WorkflowDefinition.Create("HR Bot Demo — Backend Engineer — Default Workflow");
        workflow.AddStage("Application Review", StageType.ReviewerApproval, order: 0);
        position.AssignWorkflow(workflow.Id);

        var botProject = BotProject.Create("HR Bot Demo — Backend Engineer Screening", position.Id.Value, retentionPeriodDays: 90).Value;
        var flow = botProject.AddFlow("Pre-screening", isDefault: true).Value;

        // Built end-to-front so each step can route to one already created by id.
        var screenOut = flow.AddStep(
            StepType.SendMessage,
            "Thanks for your interest! Based on your answers, we won't be moving forward with your application at this time.",
            order: 3, isFinalStep: true, isScreenOut: true).Value;

        var complete = flow.AddStep(
            StepType.SendMessage,
            "Thanks so much! We've received your answers and our team will be in touch soon.",
            order: 2, isFinalStep: true).Value;

        var waitResponse = flow.AddStep(
            StepType.WaitResponse,
            "Great! What's your expected notice period, in weeks?",
            order: 1, captureVariableKey: "noticePeriodWeeks").Value;
        waitResponse.AddRoute(StepRoute.ToStep(ConditionType.NoCondition, [], complete.Id));

        var greeting = flow.AddStep(
            StepType.SendQuickReply,
            "Hi! Thanks for applying to this role. Do you have a few minutes for some quick screening questions?",
            order: 0, quickReplyOptions: ["Yes", "No"]).Value;
        greeting.AddRoute(StepRoute.ToStep(ConditionType.HasOnlyKeyword, ["No"], screenOut.Id));
        greeting.AddRoute(StepRoute.ToStep(ConditionType.HasOnlyKeyword, ["Yes"], waitResponse.Id));

        // Same demo position carries both assessments — a real position would plausibly
        // have more than one stage configured, and it avoids a second throwaway Position.
        var caseBotProject = CaseBotProject.Create(
            "HR Bot Demo — Backend Engineer Video Interview", position.Id.Value, retakesAllowed: 1, retentionPeriodDays: 90).Value;
        var caseFlow = caseBotProject.AddFlow("Video Screening", isDefault: true).Value;
        caseFlow.AddStep(
            VideoStepType.RecordVideoAnswer,
            "Tell us about a challenging technical problem you've solved recently.",
            order: 0, preparationTimeSeconds: 10, recordingTimeSeconds: 90);

        context.Set<WorkflowDefinition>().Add(workflow);
        context.Set<Position>().Add(position);
        context.Set<BotProject>().Add(botProject);
        context.Set<CaseBotProject>().Add(caseBotProject);

        await context.SaveChangesAsync(cancellationToken);
    }
}
