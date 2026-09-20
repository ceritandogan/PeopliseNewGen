using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.HrBot.Application.Conversations.Commands;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class RequestConversationDataDeletionCommandHandlerTests
{
    private static Conversation CreateConversation() =>
        Conversation.Start(
            BotProjectId.New(), Guid.NewGuid(), ConversationInterface.WebChat,
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public async Task Anonymizes_on_consent_withdrawal()
    {
        var conversation = CreateConversation();
        conversation.RecordStep(Guid.NewGuid(), "I make 80000 right now.", DateTimeOffset.UtcNow);

        var conversations = Substitute.For<IRepository<Conversation, ConversationId>>();
        conversations.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new RequestConversationDataDeletionCommandHandler(conversations, unitOfWork);

        var result = await handler.Handle(
            new RequestConversationDataDeletionCommand(conversation.Id.Value, ConversationDataDeletionReason.ConsentWithdrawn, "No longer interested."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        conversation.CandidateId.Should().Be(Guid.Empty);
        conversation.Logs.Should().OnlyContain(l => l.CandidateResponse == null);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Uses_ExpireRetention_for_the_RetentionExpired_reason()
    {
        var conversation = CreateConversation();
        var conversations = Substitute.For<IRepository<Conversation, ConversationId>>();
        conversations.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        var handler = new RequestConversationDataDeletionCommandHandler(conversations, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new RequestConversationDataDeletionCommand(conversation.Id.Value, ConversationDataDeletionReason.RetentionExpired, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        conversation.Status.Should().Be(ConversationStatus.RetentionExpired);
    }

    [Fact]
    public async Task Returns_NotFound_for_a_missing_conversation()
    {
        var conversations = Substitute.For<IRepository<Conversation, ConversationId>>();
        conversations.GetByIdAsync(Arg.Any<ConversationId>(), Arg.Any<CancellationToken>()).Returns((Conversation?)null);
        var handler = new RequestConversationDataDeletionCommandHandler(conversations, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new RequestConversationDataDeletionCommand(Guid.NewGuid(), ConversationDataDeletionReason.RetentionExpired, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conversation.NotFound");
    }
}
