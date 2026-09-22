using FluentValidation;
using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.BotProjects.Commands;

public sealed record CreateBotProjectCommand(string Name, Guid PositionId, int RetentionPeriodDays) : IRequest<Result<Guid>>;

public sealed class CreateBotProjectCommandValidator : AbstractValidator<CreateBotProjectCommand>
{
    public CreateBotProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.RetentionPeriodDays).GreaterThan(0);
    }
}

public sealed class CreateBotProjectCommandHandler : IRequestHandler<CreateBotProjectCommand, Result<Guid>>
{
    private readonly IRepository<BotProject, BotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBotProjectCommandHandler(IRepository<BotProject, BotProjectId> projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateBotProjectCommand request, CancellationToken cancellationToken)
    {
        var createResult = BotProject.Create(request.Name, request.PositionId, request.RetentionPeriodDays);
        if (createResult.IsFailure)
            return Result.Failure<Guid>(createResult.Error);

        var project = createResult.Value;
        await _projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(project.Id.Value);
    }
}
