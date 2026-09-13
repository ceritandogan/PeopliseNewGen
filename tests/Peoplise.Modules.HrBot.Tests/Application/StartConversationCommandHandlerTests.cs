using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.HrBot.Application.Conversations.Commands;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class StartConversationCommandHandlerTests
{
    private static (
        IRepository<BotProject, BotProjectId> BotProjects,
        IRepository<Conversation, ConversationId> Conversations,
        IUnitOfWork UnitOfWork,
        StartConversationCommandHandler Handler) CreateHandler()
    {
        var botProjects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        var conversations = Substitute.For<IRepository<Conversation, ConversationId>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new StartConversationCommandHandler(botProjects, conversations, unitOfWork);
        return (botProjects, conversations, unitOfWork, handler);
    }

    [Fact]
    public async Task Returns_NotFound_when_the_bot_project_does_not_exist()
    {
        var (botProjects, _, _, handler) = CreateHandler();
        botProjects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns((BotProject?)null);

        var result = await handler.Handle(
            new StartConversationCommand(Guid.NewGuid(), Guid.NewGuid(), ConversationInterface.WebChat), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.NotFound");
    }

    [Fact]
    public async Task Returns_a_conflict_when_the_project_has_no_default_flow()
    {
        var (botProjects, _, _, handler) = CreateHandler();
        var project = BotProject.Create("Screening Bot", Guid.NewGuid());
        botProjects.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var result = await handler.Handle(
            new StartConversationCommand(project.Id.Value, Guid.NewGuid(), ConversationInterface.WebChat), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.NoDefaultFlow");
    }

    [Fact]
    public async Task Starts_the_conversation_on_the_default_flows_first_step()
    {
        var (botProjects, conversations, unitOfWork, handler) = CreateHandler();
        var project = BotProject.Create("Screening Bot", Guid.NewGuid());
        var flow = project.AddFlow("Main Flow", isDefault: true).Value;
        var firstStep = flow.AddStep(StepType.SendMessage, "Welcome!", order: 0).Value;
        botProjects.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var result = await handler.Handle(
            new StartConversationCommand(project.Id.Value, Guid.NewGuid(), ConversationInterface.WebChat), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await conversations.Received(1).AddAsync(
            Arg.Is<Conversation>(c => c.CurrentFlowId == flow.Id && c.CurrentStepId == firstStep.Id),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
