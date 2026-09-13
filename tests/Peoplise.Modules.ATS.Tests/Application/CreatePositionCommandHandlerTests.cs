using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.ATS.Application.Positions.Commands;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Application;

public class CreatePositionCommandHandlerTests
{
    [Fact]
    public async Task Adds_the_new_position_and_saves()
    {
        var repository = Substitute.For<IRepository<Position, PositionId>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new CreatePositionCommandHandler(repository, unitOfWork);

        var command = new CreatePositionCommand(
            "Backend Engineer", "Engineering", "Istanbul", "Turkey",
            WorkMode.Hybrid, SeniorityLevel.Senior, EmploymentType.FullTime);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).AddAsync(
            Arg.Is<Position>(p => p.Title == "Backend Engineer" && p.Department == "Engineering"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
