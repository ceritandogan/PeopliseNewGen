using FluentValidation;
using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.BotProjects.Commands;

public sealed record AddFlowCommand(Guid BotProjectId, string Name, bool IsDefault) : IRequest<Result<Guid>>;

public sealed class AddFlowCommandValidator : AbstractValidator<AddFlowCommand>
{
    public AddFlowCommandValidator()
    {
        RuleFor(x => x.BotProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class AddFlowCommandHandler : IRequestHandler<AddFlowCommand, Result<Guid>>
{
    private readonly IRepository<BotProject, BotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public AddFlowCommandHandler(IRepository<BotProject, BotProjectId> projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddFlowCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(BotProjectId.From(request.BotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<Guid>(Error.NotFound("BotProject.NotFound", $"No project '{request.BotProjectId}' was found."));

        var result = project.AddFlow(request.Name, request.IsDefault);
        if (result.IsFailure)
            return Result.Failure<Guid>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(result.Value.Id);
    }
}
