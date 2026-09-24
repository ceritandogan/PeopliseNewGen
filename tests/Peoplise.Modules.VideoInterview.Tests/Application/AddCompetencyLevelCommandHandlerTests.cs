using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.Entities;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Application;

public class AddCompetencyLevelCommandHandlerTests
{
    private static CaseBotProject CreateProjectWithCompetency(out Guid competencyId)
    {
        var project = CaseBotProject.Create("Backend Case Study", Guid.NewGuid(), retakesAllowed: 1, retentionPeriodDays: 90).Value;
        var competency = project.AddCompetency("Problem Solving", null);
        competencyId = competency.Id;
        return project;
    }

    [Fact]
    public async Task Returns_NotFound_when_the_project_does_not_exist()
    {
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(Arg.Any<CaseBotProjectId>(), Arg.Any<CancellationToken>()).Returns((CaseBotProject?)null);
        var handler = new AddCompetencyLevelCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddCompetencyLevelCommand(Guid.NewGuid(), Guid.NewGuid(), 3, "Solves moderately complex problems independently."),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CaseBotProject.NotFound");
    }

    [Fact]
    public async Task Returns_CompetencyNotFound_when_the_competency_is_not_part_of_the_project()
    {
        var project = CaseBotProject.Create("Backend Case Study", Guid.NewGuid(), retakesAllowed: 1, retentionPeriodDays: 90).Value;
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(Arg.Any<CaseBotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var handler = new AddCompetencyLevelCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddCompetencyLevelCommand(project.Id.Value, Guid.NewGuid(), 3, "Solves moderately complex problems independently."),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CaseBotProject.CompetencyNotFound");
    }

    [Fact]
    public async Task Adds_a_level_and_saves()
    {
        var project = CreateProjectWithCompetency(out var competencyId);
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(Arg.Any<CaseBotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddCompetencyLevelCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(
            new AddCompetencyLevelCommand(project.Id.Value, competencyId, 3, "Solves moderately complex problems independently."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var competency = project.FindCompetency(competencyId)!;
        competency.Levels.Should().ContainSingle(l => l.Id == result.Value && l.Level == 3);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Rejects_a_level_outside_the_1_to_5_range(int level)
    {
        var validator = new AddCompetencyLevelCommandValidator();

        var result = await validator.ValidateAsync(new AddCompetencyLevelCommand(Guid.NewGuid(), Guid.NewGuid(), level, "Some description."));

        result.IsValid.Should().BeFalse();
    }
}
