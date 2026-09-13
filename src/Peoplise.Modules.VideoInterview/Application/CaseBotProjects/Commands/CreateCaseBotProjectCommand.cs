using FluentValidation;
using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;

public sealed record CreateCaseBotProjectCommand(
    string Name,
    Guid PositionId,
    int RetakesAllowed,
    int RetentionPeriodDays) : IRequest<Result<Guid>>;

public sealed class CreateCaseBotProjectCommandValidator : AbstractValidator<CreateCaseBotProjectCommand>
{
    public CreateCaseBotProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.RetakesAllowed).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RetentionPeriodDays).GreaterThan(0);
    }
}

public sealed class CreateCaseBotProjectCommandHandler : IRequestHandler<CreateCaseBotProjectCommand, Result<Guid>>
{
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCaseBotProjectCommandHandler(IRepository<CaseBotProject, CaseBotProjectId> projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateCaseBotProjectCommand request, CancellationToken cancellationToken)
    {
        var createResult = CaseBotProject.Create(request.Name, request.PositionId, request.RetakesAllowed, request.RetentionPeriodDays);
        if (createResult.IsFailure)
            return Result.Failure<Guid>(createResult.Error);

        var project = createResult.Value;
        await _projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(project.Id.Value);
    }
}
