using FluentValidation;
using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.Conversations.Commands;

public sealed record StartConversationCommand(
    Guid BotProjectId,
    Guid CandidateId,
    ConversationInterface Interface) : IRequest<Result<Guid>>;

public sealed class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationCommandValidator()
    {
        RuleFor(x => x.BotProjectId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
    }
}

public sealed class StartConversationCommandHandler : IRequestHandler<StartConversationCommand, Result<Guid>>
{
    private readonly IRepository<BotProject, BotProjectId> _botProjects;
    private readonly IRepository<Conversation, ConversationId> _conversations;
    private readonly IUnitOfWork _unitOfWork;

    public StartConversationCommandHandler(
        IRepository<BotProject, BotProjectId> botProjects,
        IRepository<Conversation, ConversationId> conversations,
        IUnitOfWork unitOfWork)
    {
        _botProjects = botProjects;
        _conversations = conversations;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        var botProject = await _botProjects.GetByIdAsync(BotProjectId.From(request.BotProjectId), cancellationToken);
        if (botProject is null)
            return Result.Failure<Guid>(Error.NotFound("BotProject.NotFound", $"No bot project '{request.BotProjectId}' was found."));

        var defaultFlow = botProject.DefaultFlow();
        if (defaultFlow is null)
            return Result.Failure<Guid>(Error.Conflict("BotProject.NoDefaultFlow", "This bot project has no default flow to start from."));

        var firstStep = defaultFlow.FirstStep();
        if (firstStep is null)
            return Result.Failure<Guid>(Error.Conflict("BotProject.EmptyDefaultFlow", "The default flow has no steps."));

        var conversation = Conversation.Start(
            botProject.Id, request.CandidateId, request.Interface, defaultFlow.Id, firstStep.Id, DateTimeOffset.UtcNow);

        await _conversations.AddAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(conversation.Id.Value);
    }
}
