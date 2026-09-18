using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Peoplise.Infrastructure.Modules;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.HrBot.Application.Conversations.Commands;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class StartConversationCommandHandlerTests
{
    private static (
        AppDbContext Context,
        Guid TenantId,
        IRepository<Conversation, ConversationId> Conversations,
        IUnitOfWork UnitOfWork,
        StartConversationCommandHandler Handler) CreateHandler()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId.From(tenantId));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options, tenantContext, new ModuleAssemblyRegistry([typeof(BotProject).Assembly]));

        var conversations = Substitute.For<IRepository<Conversation, ConversationId>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new StartConversationCommandHandler(context, conversations, unitOfWork);
        return (context, tenantId, conversations, unitOfWork, handler);
    }

    [Fact]
    public async Task Returns_NotFound_when_no_bot_project_is_configured_for_the_position()
    {
        var (_, _, _, _, handler) = CreateHandler();

        var result = await handler.Handle(
            new StartConversationCommand(Guid.NewGuid(), Guid.NewGuid(), ConversationInterface.WebChat), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.NotFound");
    }

    [Fact]
    public async Task Returns_a_conflict_when_the_project_has_no_default_flow()
    {
        var (context, tenantId, _, _, handler) = CreateHandler();
        var positionId = Guid.NewGuid();
        var project = BotProject.Create("Screening Bot", positionId);
        project.TenantId = tenantId;
        context.Set<BotProject>().Add(project);
        await context.SaveChangesAsync();

        var result = await handler.Handle(
            new StartConversationCommand(positionId, Guid.NewGuid(), ConversationInterface.WebChat), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.NoDefaultFlow");
    }

    [Fact]
    public async Task Starts_the_conversation_on_the_default_flows_first_step_and_returns_its_content()
    {
        var (context, tenantId, conversations, unitOfWork, handler) = CreateHandler();
        var positionId = Guid.NewGuid();
        var project = BotProject.Create("Screening Bot", positionId);
        project.TenantId = tenantId;
        var flow = project.AddFlow("Main Flow", isDefault: true).Value;
        var firstStep = flow.AddStep(StepType.SendMessage, "Welcome!", order: 0).Value;
        context.Set<BotProject>().Add(project);
        await context.SaveChangesAsync();

        var result = await handler.Handle(
            new StartConversationCommand(positionId, Guid.NewGuid(), ConversationInterface.WebChat), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentStep.StepId.Should().Be(firstStep.Id);
        result.Value.CurrentStep.Content.Should().Be("Welcome!");
        await conversations.Received(1).AddAsync(
            Arg.Is<Conversation>(c => c.CurrentFlowId == flow.Id && c.CurrentStepId == firstStep.Id),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
