using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Application;

public class AddCompetencyIndicatorCommandHandlerTests
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
        var handler = new AddCompetencyIndicatorCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddCompetencyIndicatorCommand(Guid.NewGuid(), Guid.NewGuid(), "Breaks problems into smaller steps."),
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
        var handler = new AddCompetencyIndicatorCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddCompetencyIndicatorCommand(project.Id.Value, Guid.NewGuid(), "Breaks problems into smaller steps."),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CaseBotProject.CompetencyNotFound");
    }

    [Fact]
    public async Task Adds_an_indicator_and_saves()
    {
        var project = CreateProjectWithCompetency(out var competencyId);
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(Arg.Any<CaseBotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddCompetencyIndicatorCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(
            new AddCompetencyIndicatorCommand(project.Id.Value, competencyId, "Breaks problems into smaller steps."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var competency = project.FindCompetency(competencyId)!;
        competency.Indicators.Should().ContainSingle(i => i.Id == result.Value && i.Description == "Breaks problems into smaller steps.");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
