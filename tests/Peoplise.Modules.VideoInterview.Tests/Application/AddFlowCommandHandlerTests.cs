using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Application;

public class AddFlowCommandHandlerTests
{
    [Fact]
    public async Task Returns_NotFound_when_the_project_does_not_exist()
    {
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(Arg.Any<CaseBotProjectId>(), Arg.Any<CancellationToken>()).Returns((CaseBotProject?)null);
        var handler = new AddFlowCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new AddFlowCommand(Guid.NewGuid(), "Main Flow", true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CaseBotProject.NotFound");
    }

    [Fact]
    public async Task Adds_a_flow_and_saves()
    {
        var project = CaseBotProject.Create("Backend Case Study", Guid.NewGuid(), retakesAllowed: 1, retentionPeriodDays: 90).Value;
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(Arg.Any<CaseBotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddFlowCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(new AddFlowCommand(project.Id.Value, "Main Flow", true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Flows.Should().ContainSingle(f => f.Id == result.Value && f.Name == "Main Flow" && f.IsDefault);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_a_second_default_flow_without_saving()
    {
        var project = CaseBotProject.Create("Backend Case Study", Guid.NewGuid(), retakesAllowed: 1, retentionPeriodDays: 90).Value;
        project.AddFlow("Main Flow", isDefault: true);
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(Arg.Any<CaseBotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddFlowCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(new AddFlowCommand(project.Id.Value, "Backup Flow", true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CaseBotProject.DefaultFlowAlreadySet");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
