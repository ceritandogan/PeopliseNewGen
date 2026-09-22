using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Peoplise.Infrastructure.Modules;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.HrBot.Application.Conversations.Queries;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.MultiTenancy;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class GetConversationHistoryQueryTests
{
    private static (AppDbContext Context, Guid TenantId, GetConversationHistoryQueryHandler Handler) CreateHandler()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId.From(tenantId));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options, tenantContext, new ModuleAssemblyRegistry([typeof(BotProject).Assembly]));

        return (context, tenantId, new GetConversationHistoryQueryHandler(context));
    }

    [Fact]
    public async Task Returns_NotFound_for_a_missing_conversation()
    {
        var (_, _, handler) = CreateHandler();

        var result = await handler.Handle(new GetConversationHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conversation.NotFound");
    }

    [Fact]
    public async Task Resolves_each_logged_steps_bot_message_from_the_project_flow()
    {
        var (context, tenantId, handler) = CreateHandler();
        var project = BotProject.Create("Screening Bot", Guid.NewGuid(), retentionPeriodDays: 90).Value;
        project.TenantId = tenantId;
        var flow = project.AddFlow("Main", isDefault: true).Value;
        var step = flow.AddStep(StepType.SendMessage, "What's your salary expectation?", order: 0).Value;
        context.Set<BotProject>().Add(project);

        var conversation = Conversation.Start(
            project.Id, Guid.NewGuid(), ConversationInterface.WebChat, flow.Id, step.Id, DateTimeOffset.UtcNow);
        conversation.TenantId = tenantId;
        conversation.RecordStep(step.Id, "80000", DateTimeOffset.UtcNow);
        context.Set<Conversation>().Add(conversation);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new GetConversationHistoryQuery(conversation.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var entry = result.Value.Logs.Should().ContainSingle().Subject;
        entry.BotMessage.Should().Be("What's your salary expectation?");
        entry.CandidateResponse.Should().Be("80000");
    }

    [Fact]
    public async Task Resolves_a_step_from_a_non_default_flow_the_conversation_switched_into()
    {
        var (context, tenantId, handler) = CreateHandler();
        var project = BotProject.Create("Screening Bot", Guid.NewGuid(), retentionPeriodDays: 90).Value;
        project.TenantId = tenantId;
        var defaultFlow = project.AddFlow("Main", isDefault: true).Value;
        defaultFlow.AddStep(StepType.SendMessage, "Welcome!", order: 0);
        var otherFlow = project.AddFlow("Follow-up", isDefault: false).Value;
        var otherStep = otherFlow.AddStep(StepType.SendMessage, "One more thing — location?", order: 0).Value;
        context.Set<BotProject>().Add(project);

        var conversation = Conversation.Start(
            project.Id, Guid.NewGuid(), ConversationInterface.WebChat, defaultFlow.Id, otherStep.Id, DateTimeOffset.UtcNow);
        conversation.TenantId = tenantId;
        conversation.RecordStep(otherStep.Id, "Istanbul", DateTimeOffset.UtcNow);
        context.Set<Conversation>().Add(conversation);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new GetConversationHistoryQuery(conversation.Id.Value), CancellationToken.None);

        result.Value.Logs.Should().ContainSingle().Which.BotMessage.Should().Be("One more thing — location?");
    }

    [Fact]
    public async Task A_log_for_a_step_no_longer_found_in_the_project_gets_a_null_bot_message_instead_of_failing()
    {
        var (context, tenantId, handler) = CreateHandler();
        var project = BotProject.Create("Screening Bot", Guid.NewGuid(), retentionPeriodDays: 90).Value;
        project.TenantId = tenantId;
        context.Set<BotProject>().Add(project);

        var conversation = Conversation.Start(
            project.Id, Guid.NewGuid(), ConversationInterface.WebChat, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        conversation.TenantId = tenantId;
        var unknownStepId = Guid.NewGuid();
        conversation.RecordStep(unknownStepId, "some answer", DateTimeOffset.UtcNow);
        context.Set<Conversation>().Add(conversation);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new GetConversationHistoryQuery(conversation.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Logs.Should().ContainSingle().Which.BotMessage.Should().BeNull();
    }

    [Fact]
    public async Task Includes_captured_variables()
    {
        var (context, tenantId, handler) = CreateHandler();
        var project = BotProject.Create("Screening Bot", Guid.NewGuid(), retentionPeriodDays: 90).Value;
        project.TenantId = tenantId;
        context.Set<BotProject>().Add(project);

        var conversation = Conversation.Start(
            project.Id, Guid.NewGuid(), ConversationInterface.WebChat, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        conversation.TenantId = tenantId;
        conversation.SetVariable("salary_expectation", "80000", DateTimeOffset.UtcNow);
        context.Set<Conversation>().Add(conversation);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new GetConversationHistoryQuery(conversation.Id.Value), CancellationToken.None);

        result.Value.Variables.Should().ContainSingle(v => v.Key == "salary_expectation" && v.Value == "80000");
    }
}
