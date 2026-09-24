using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.HrBot.Application.BotProjects.Commands;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class AddVariableCommandHandlerTests
{
    [Fact]
    public async Task Returns_NotFound_when_the_project_does_not_exist()
    {
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns((BotProject?)null);
        var handler = new AddVariableCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new AddVariableCommand(Guid.NewGuid(), "noticePeriodWeeks", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.NotFound");
    }

    [Fact]
    public async Task Adds_a_variable_and_saves()
    {
        var project = BotProject.Create("Backend Screening Bot", Guid.NewGuid(), retentionPeriodDays: 90).Value;
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddVariableCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(
            new AddVariableCommand(project.Id.Value, "noticePeriodWeeks", "Candidate's stated notice period, in weeks."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Variables.Should().ContainSingle(v => v.Id == result.Value && v.Key == "noticePeriodWeeks");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Allows_a_second_variable_with_the_same_key_no_uniqueness_guard()
    {
        var project = BotProject.Create("Backend Screening Bot", Guid.NewGuid(), retentionPeriodDays: 90).Value;
        project.AddVariable("noticePeriodWeeks", null);
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var handler = new AddVariableCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new AddVariableCommand(project.Id.Value, "noticePeriodWeeks", "A duplicate key."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Variables.Should().HaveCount(2);
    }
}
