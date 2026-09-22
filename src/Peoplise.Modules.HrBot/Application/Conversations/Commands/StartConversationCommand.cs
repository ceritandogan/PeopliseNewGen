using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.Conversations.Commands;

/// <summary>
/// Keyed by <see cref="PositionId"/>, not <c>BotProjectId</c>: the candidate app already
/// thinks entirely in terms of positions (<c>/apply/:positionId</c>,
/// <c>SubmitCandidateApplicationRequest.PositionId</c>) — it has no reason to know a
/// <c>BotProject</c> exists as its own concept.
/// </summary>
public sealed record StartConversationCommand(
    Guid PositionId,
    Guid CandidateId,
    ConversationInterface Interface) : IRequest<Result<StartConversationResult>>;

public sealed record StartConversationResult(Guid ConversationId, ConversationStepContent CurrentStep);

public sealed class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationCommandValidator()
    {
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
    }
}

public sealed class StartConversationCommandHandler : IRequestHandler<StartConversationCommand, Result<StartConversationResult>>
{
    private readonly AppDbContext _context;
    private readonly IRepository<Conversation, ConversationId> _conversations;
    private readonly IUnitOfWork _unitOfWork;

    public StartConversationCommandHandler(
        AppDbContext context,
        IRepository<Conversation, ConversationId> conversations,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _conversations = conversations;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StartConversationResult>> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        // Lookup by PositionId, not by BotProjectId — beyond IRepository's simple by-id
        // contract (see its remarks), so this reads via AppDbContext directly rather
        // than adding a one-off repository for a single query. A position can have more
        // than one BotProject (no uniqueness guard); most-recently-created wins — a
        // second project is expected to mean "re-run with a new config", not "also start
        // conversations under the old one".
        var botProject = await _context.Set<BotProject>()
            .Where(p => p.PositionId == request.PositionId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (botProject is null)
        {
            return Result.Failure<StartConversationResult>(
                Error.NotFound("BotProject.NotFound", $"No bot project is configured for position '{request.PositionId}'."));
        }

        var defaultFlow = botProject.DefaultFlow();
        if (defaultFlow is null)
            return Result.Failure<StartConversationResult>(Error.Conflict("BotProject.NoDefaultFlow", "This bot project has no default flow to start from."));

        var firstStep = defaultFlow.FirstStep();
        if (firstStep is null)
            return Result.Failure<StartConversationResult>(Error.Conflict("BotProject.EmptyDefaultFlow", "The default flow has no steps."));

        var conversation = Conversation.Start(
            botProject.Id, request.CandidateId, request.Interface, defaultFlow.Id, firstStep.Id, DateTimeOffset.UtcNow);

        await _conversations.AddAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new StartConversationResult(conversation.Id.Value, ConversationStepMapper.ToContent(firstStep)));
    }
}
