using FluentValidation;
using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Media;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.Cases.Commands;

/// <summary>
/// Stores a candidate's video answer and triggers background transcription. There's no
/// HTTP upload endpoint built yet (no module has controllers at this stage — see the
/// architecture doc's stage boundaries), so this takes a raw <see cref="Stream"/>
/// directly; a future controller would adapt an uploaded file into this same command.
/// </summary>
public sealed record SubmitVideoAnswerCommand(
    Guid CaseId,
    Guid StepId,
    Stream VideoContent,
    string FileName,
    string ContentType) : IRequest<Result>;

public sealed class SubmitVideoAnswerCommandValidator : AbstractValidator<SubmitVideoAnswerCommand>
{
    public SubmitVideoAnswerCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty();
    }
}

public sealed class SubmitVideoAnswerCommandHandler : IRequestHandler<SubmitVideoAnswerCommand, Result>
{
    private readonly IRepository<Case, CaseId> _cases;
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitVideoAnswerCommandHandler(
        IRepository<Case, CaseId> cases,
        IRepository<CaseBotProject, CaseBotProjectId> projects,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork)
    {
        _cases = cases;
        _projects = projects;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SubmitVideoAnswerCommand request, CancellationToken cancellationToken)
    {
        var @case = await _cases.GetByIdAsync(CaseId.From(request.CaseId), cancellationToken);
        if (@case is null)
            return Result.Failure(Error.NotFound("Case.NotFound", $"No case '{request.CaseId}' was found."));

        var project = await _projects.GetByIdAsync(@case.CaseBotProjectId, cancellationToken);
        if (project is null)
            return Result.Failure(Error.NotFound("CaseBotProject.NotFound", "The case's project could not be found."));

        var videoUrl = await _fileStorage.UploadAsync(request.VideoContent, request.FileName, request.ContentType, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var result = @case.RecordVideoAnswer(request.StepId, videoUrl, project.RetakesAllowed, now);
        if (result.IsFailure)
        {
            // Compensate: don't leave an orphaned file behind for a rejected retake.
            await _fileStorage.DeleteAsync(videoUrl, cancellationToken);
            return result;
        }

        // There's no per-turn "what's next" round trip here (unlike HrBot's branching
        // conversations): a case-bot flow is a fixed linear sequence with no server-side
        // step advancement, so the flow's last step being answered is what closes the
        // case — completion applied inline, same as HrBot closes a conversation inline
        // in ProcessUserResponseCommandHandler rather than via a separate call.
        var flow = project.FindFlow(@case.CurrentFlowId);
        var isLastStep = flow is not null && flow.Steps.OrderBy(s => s.Order).LastOrDefault()?.Id == request.StepId;
        if (isLastStep)
        {
            var completeResult = @case.Complete(now);
            if (completeResult.IsFailure)
                return completeResult;
        }

        // Dispatched from UnitOfWork.SaveChangesAsync: the VideoRecordedEvent this just
        // raised is what enqueues background transcription — see VideoRecordedEventHandler.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
