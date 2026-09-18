using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.MultiTenancy;

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
        var alreadySeeded = await context.Set<BotProject>().IgnoreQueryFilters().AnyAsync(cancellationToken);
        if (alreadySeeded) return;

        using var _ = AmbientTenantOverride.Begin(TenantId.From(DatabaseSeeder.DemoTenantId));

        var position = Position.Create(
            "HR Bot Demo — Backend Engineer", "Engineering", "Istanbul", "Turkey",
            WorkMode.Hybrid, SeniorityLevel.Mid, EmploymentType.FullTime);

        var workflow = WorkflowDefinition.Create("HR Bot Demo — Backend Engineer — Default Workflow");
        workflow.AddStage("Application Review", StageType.ReviewerApproval, order: 0);
        position.AssignWorkflow(workflow.Id);

        var botProject = BotProject.Create("HR Bot Demo — Backend Engineer Screening", position.Id.Value);
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

        context.Set<WorkflowDefinition>().Add(workflow);
        context.Set<Position>().Add(position);
        context.Set<BotProject>().Add(botProject);

        await context.SaveChangesAsync(cancellationToken);
    }
}
