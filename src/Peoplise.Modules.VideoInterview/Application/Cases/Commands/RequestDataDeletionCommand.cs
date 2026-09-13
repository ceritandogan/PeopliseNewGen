using FluentValidation;
using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Media;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.Cases.Commands;

public enum DataDeletionReason
{
    ConsentWithdrawn,
    RetentionExpired,
}

/// <summary>
/// KVKK Section G: deletes a case's media files and severs the candidate identity,
/// either because the candidate withdrew consent or because the project's retention
/// period expired — see <see cref="Domain.Aggregates.Case.WithdrawConsent"/> /
/// <see cref="Domain.Aggregates.Case.ExpireRetention"/> for what stays (scorings,
/// results) vs. what goes (media, identity).
/// </summary>
public sealed record RequestDataDeletionCommand(Guid CaseId, DataDeletionReason Reason, string? WithdrawalReason) : IRequest<Result>;

public sealed class RequestDataDeletionCommandValidator : AbstractValidator<RequestDataDeletionCommand>
{
    public RequestDataDeletionCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
    }
}

public sealed class RequestDataDeletionCommandHandler : IRequestHandler<RequestDataDeletionCommand, Result>
{
    private readonly IRepository<Case, CaseId> _cases;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;

    public RequestDataDeletionCommandHandler(IRepository<Case, CaseId> cases, IFileStorageService fileStorage, IUnitOfWork unitOfWork)
    {
        _cases = cases;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RequestDataDeletionCommand request, CancellationToken cancellationToken)
    {
        var @case = await _cases.GetByIdAsync(CaseId.From(request.CaseId), cancellationToken);
        if (@case is null)
            return Result.Failure(Error.NotFound("Case.NotFound", $"No case '{request.CaseId}' was found."));

        // Physically delete every referenced file before the domain clears the
        // references — once WithdrawConsent/ExpireRetention runs, the URLs are gone
        // from the aggregate and there'd be nothing left to delete by.
        foreach (var conversation in @case.StepConversations)
        {
            if (conversation.VideoUrl is not null)
                await _fileStorage.DeleteAsync(conversation.VideoUrl, cancellationToken);
            if (conversation.DocumentUrl is not null)
                await _fileStorage.DeleteAsync(conversation.DocumentUrl, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var result = request.Reason == DataDeletionReason.ConsentWithdrawn
            ? @case.WithdrawConsent(request.WithdrawalReason ?? "Candidate withdrew consent.", now)
            : @case.ExpireRetention(now);

        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
