using FluentValidation;
using MediatR;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Positions.Commands;

public sealed record CreatePositionCommand(
    string Title,
    string Department,
    string City,
    string Country,
    WorkMode WorkMode,
    SeniorityLevel SeniorityLevel,
    EmploymentType EmploymentType) : IRequest<Result<Guid>>;

public sealed class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreatePositionCommandHandler : IRequestHandler<CreatePositionCommand, Result<Guid>>
{
    private readonly IRepository<Position, PositionId> _positions;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePositionCommandHandler(IRepository<Position, PositionId> positions, IUnitOfWork unitOfWork)
    {
        _positions = positions;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
    {
        var position = Position.Create(
            request.Title, request.Department, request.City, request.Country,
            request.WorkMode, request.SeniorityLevel, request.EmploymentType);

        await _positions.AddAsync(position, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(position.Id.Value);
    }
}
