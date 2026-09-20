using FluentValidation;
using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.Conversations.Commands;

public enum ConversationDataDeletionReason
{
    ConsentWithdrawn,
    RetentionExpired,
}

/// <summary>
/// KVKK: anonymizes a conversation's identity and content, either because the candidate
/// withdrew consent or because the project's retention period expired — see
/// <see cref="Domain.Aggregates.Conversation.WithdrawConsent"/> /
/// <see cref="Domain.Aggregates.Conversation.ExpireRetention"/> for what's cleared.
/// Unlike VideoInterview's equivalent command, there's no file to delete first — HrBot
/// conversations hold no media, only text — so this is a direct domain call + save.
/// </summary>
public sealed record RequestConversationDataDeletionCommand(Guid ConversationId, ConversationDataDeletionReason Reason, string? WithdrawalReason)
    : IRequest<Result>;

public sealed class RequestConversationDataDeletionCommandValidator : AbstractValidator<RequestConversationDataDeletionCommand>
{
    public RequestConversationDataDeletionCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}

public sealed class RequestConversationDataDeletionCommandHandler : IRequestHandler<RequestConversationDataDeletionCommand, Result>
{
    private readonly IRepository<Conversation, ConversationId> _conversations;
    private readonly IUnitOfWork _unitOfWork;

    public RequestConversationDataDeletionCommandHandler(IRepository<Conversation, ConversationId> conversations, IUnitOfWork unitOfWork)
    {
        _conversations = conversations;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RequestConversationDataDeletionCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversations.GetByIdAsync(ConversationId.From(request.ConversationId), cancellationToken);
        if (conversation is null)
            return Result.Failure(Error.NotFound("Conversation.NotFound", $"No conversation '{request.ConversationId}' was found."));

        var now = DateTimeOffset.UtcNow;
        var result = request.Reason == ConversationDataDeletionReason.ConsentWithdrawn
            ? conversation.WithdrawConsent(request.WithdrawalReason ?? "Candidate withdrew consent.", now)
            : conversation.ExpireRetention(now);

        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
