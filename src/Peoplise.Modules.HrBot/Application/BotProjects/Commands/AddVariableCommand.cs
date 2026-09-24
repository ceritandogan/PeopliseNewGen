using FluentValidation;
using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.BotProjects.Commands;

public sealed record AddVariableCommand(Guid BotProjectId, string Key, string? Description) : IRequest<Result<Guid>>;

public sealed class AddVariableCommandValidator : AbstractValidator<AddVariableCommand>
{
    public AddVariableCommandValidator()
    {
        RuleFor(x => x.BotProjectId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
    }
}

public sealed class AddVariableCommandHandler : IRequestHandler<AddVariableCommand, Result<Guid>>
{
    private readonly IRepository<BotProject, BotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public AddVariableCommandHandler(IRepository<BotProject, BotProjectId> projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddVariableCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(BotProjectId.From(request.BotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<Guid>(Error.NotFound("BotProject.NotFound", $"No project '{request.BotProjectId}' was found."));

        var variable = project.AddVariable(request.Key, request.Description);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(variable.Id);
    }
}
