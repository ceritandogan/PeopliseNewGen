using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.HrBot.Application.BotProjects.Commands;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class AddFlowCommandHandlerTests
{
    [Fact]
    public async Task Returns_NotFound_when_the_project_does_not_exist()
    {
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns((BotProject?)null);
        var handler = new AddFlowCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new AddFlowCommand(Guid.NewGuid(), "Pre-screening", true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.NotFound");
    }

    [Fact]
    public async Task Adds_a_flow_and_saves()
    {
        var project = BotProject.Create("Backend Screening Bot", Guid.NewGuid(), retentionPeriodDays: 90).Value;
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddFlowCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(new AddFlowCommand(project.Id.Value, "Pre-screening", true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Flows.Should().ContainSingle(f => f.Id == result.Value && f.Name == "Pre-screening" && f.IsDefault);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_a_second_default_flow_without_saving()
    {
        var project = BotProject.Create("Backend Screening Bot", Guid.NewGuid(), retentionPeriodDays: 90).Value;
        project.AddFlow("Pre-screening", isDefault: true);
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddFlowCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(new AddFlowCommand(project.Id.Value, "Backup Flow", true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.DefaultFlowAlreadySet");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
