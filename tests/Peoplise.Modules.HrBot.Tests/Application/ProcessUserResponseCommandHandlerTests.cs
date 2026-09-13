using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.HrBot.Application.Conversations.Commands;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.Events;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class ProcessUserResponseCommandHandlerTests
{
    private static (
        IRepository<Conversation, ConversationId> Conversations,
        IRepository<BotProject, BotProjectId> BotProjects,
        IUnitOfWork UnitOfWork,
        ProcessUserResponseCommandHandler Handler) CreateHandler()
    {
        var conversations = Substitute.For<IRepository<Conversation, ConversationId>>();
        var botProjects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new ProcessUserResponseCommandHandler(conversations, botProjects, unitOfWork);
        return (conversations, botProjects, unitOfWork, handler);
    }

    private static (BotProject Project, Flow Flow, Step CaptureStep, Step NextStep) BuildProjectWithCaptureStep()
    {
        var project = BotProject.Create("Screening Bot", Guid.NewGuid());
        var flow = project.AddFlow("Main Flow", isDefault: true).Value;
        var nextStep = flow.AddStep(StepType.SendMessage, "Thanks!", order: 1, isFinalStep: true).Value;
        var captureStep = flow.AddStep(
            StepType.WaitResponse, "What's your salary expectation?", order: 0, captureVariableKey: "salary_expectation").Value;
        captureStep.AddRoute(StepRoute.ToStep(ConditionType.NoCondition, [], nextStep.Id));
        return (project, flow, captureStep, nextStep);
    }

    [Fact]
    public async Task Captures_the_candidates_free_text_answer_into_the_steps_variable()
    {
        var (conversations, botProjects, _, handler) = CreateHandler();
        var (project, flow, captureStep, _) = BuildProjectWithCaptureStep();
        var conversation = Conversation.Start(
            project.Id, Guid.NewGuid(), ConversationInterface.WebChat, flow.Id, captureStep.Id, DateTimeOffset.UtcNow);

        conversations.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        botProjects.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var result = await handler.Handle(
            new ProcessUserResponseCommand(conversation.Id.Value, "85000 TRY"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        conversation.Variables.Should().ContainSingle(v => v.Key == "salary_expectation" && v.Value == "85000 TRY");
    }

    [Fact]
    public async Task Advances_to_the_next_step_via_a_NoCondition_route_and_completes_on_reaching_a_final_step()
    {
        var (conversations, botProjects, unitOfWork, handler) = CreateHandler();
        var (project, flow, captureStep, nextStep) = BuildProjectWithCaptureStep();
        var conversation = Conversation.Start(
            project.Id, Guid.NewGuid(), ConversationInterface.WebChat, flow.Id, captureStep.Id, DateTimeOffset.UtcNow);

        conversations.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        botProjects.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var result = await handler.Handle(new ProcessUserResponseCommand(conversation.Id.Value, "85000"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RouteMatched.Should().BeTrue();
        conversation.CurrentStepId.Should().Be(nextStep.Id);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Screens_out_the_candidate_when_the_route_leads_to_a_screen_out_final_step()
    {
        var (conversations, botProjects, _, handler) = CreateHandler();
        var project = BotProject.Create("Screening Bot", Guid.NewGuid());
        var flow = project.AddFlow("Main Flow", isDefault: true).Value;
        var screenOutStep = flow.AddStep(StepType.SendMessage, "Sorry, not a fit.", order: 1, isFinalStep: true, isScreenOut: true).Value;
        var askStep = flow.AddStep(StepType.SendQuickReply, "Willing to relocate?", order: 0).Value;
        askStep.AddRoute(StepRoute.ToStep(ConditionType.HasOnlyKeyword, ["Hayır"], screenOutStep.Id));

        var conversation = Conversation.Start(
            project.Id, Guid.NewGuid(), ConversationInterface.WebChat, flow.Id, askStep.Id, DateTimeOffset.UtcNow);
        conversations.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        botProjects.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        // First turn: answers the quick reply, gets routed to the screen-out step.
        await handler.Handle(new ProcessUserResponseCommand(conversation.Id.Value, "Hayır"), CancellationToken.None);
        conversation.CurrentStepId.Should().Be(screenOutStep.Id);
        conversation.Status.Should().Be(ConversationStatus.InProgress, "arriving at a final step doesn't close it until the next turn processes it");

        // Second turn: the engine processes the (now current) final step and closes the conversation.
        var result = await handler.Handle(new ProcessUserResponseCommand(conversation.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        conversation.Status.Should().Be(ConversationStatus.ScreenedOut);
    }

    [Fact]
    public async Task Logs_an_unmatched_question_at_a_FaqEngine_step_when_the_knowledgebase_has_no_match()
    {
        var (conversations, botProjects, _, handler) = CreateHandler();
        var project = BotProject.Create("Screening Bot", Guid.NewGuid());
        var flow = project.AddFlow("Main Flow", isDefault: true).Value;
        var faqStep = flow.AddStep(StepType.FaqEngine, "Ask me anything", order: 0).Value;
        faqStep.AddRoute(StepRoute.ToStep(ConditionType.NoCondition, [], faqStep.Id));

        var conversation = Conversation.Start(
            project.Id, Guid.NewGuid(), ConversationInterface.WebChat, flow.Id, faqStep.Id, DateTimeOffset.UtcNow);
        conversations.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        botProjects.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var result = await handler.Handle(
            new ProcessUserResponseCommand(conversation.Id.Value, "Do you sponsor visas?"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FaqAnswer.Should().BeNull();
        conversation.DomainEvents.Should().Contain(e => e is UnmatchedQuestionLoggedEvent);
    }

    [Fact]
    public async Task Returns_the_matched_answer_at_a_FaqEngine_step_when_the_knowledgebase_has_a_match()
    {
        var (conversations, botProjects, _, handler) = CreateHandler();
        var project = BotProject.Create("Screening Bot", Guid.NewGuid());
        var answer = new KnowledgebaseAnswer(Guid.NewGuid(), "Yes, we sponsor work visas.");
        project.Knowledgebase.AddQuestion(new KnowledgebaseQuestion(Guid.NewGuid(), "Visa sponsorship?", ["visa"], answer));
        var flow = project.AddFlow("Main Flow", isDefault: true).Value;
        var faqStep = flow.AddStep(StepType.FaqEngine, "Ask me anything", order: 0).Value;
        faqStep.AddRoute(StepRoute.ToStep(ConditionType.NoCondition, [], faqStep.Id));

        var conversation = Conversation.Start(
            project.Id, Guid.NewGuid(), ConversationInterface.WebChat, flow.Id, faqStep.Id, DateTimeOffset.UtcNow);
        conversations.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        botProjects.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var result = await handler.Handle(
            new ProcessUserResponseCommand(conversation.Id.Value, "Do you offer visa sponsorship?"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FaqAnswer.Should().Be("Yes, we sponsor work visas.");
    }

    [Fact]
    public async Task Returns_NotFound_when_the_conversation_does_not_exist()
    {
        var (conversations, _, _, handler) = CreateHandler();
        conversations.GetByIdAsync(Arg.Any<ConversationId>(), Arg.Any<CancellationToken>()).Returns((Conversation?)null);

        var result = await handler.Handle(new ProcessUserResponseCommand(Guid.NewGuid(), "hi"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conversation.NotFound");
    }
}
