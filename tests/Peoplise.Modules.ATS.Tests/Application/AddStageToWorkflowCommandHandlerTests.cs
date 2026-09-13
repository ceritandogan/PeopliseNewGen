using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.ATS.Application.Workflows.Commands;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Application;

public class AddStageToWorkflowCommandHandlerTests
{
    [Fact]
    public async Task Returns_NotFound_when_the_workflow_does_not_exist()
    {
        var repository = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowDefinition?)null);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddStageToWorkflowCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new AddStageToWorkflowCommand(Guid.NewGuid(), "Screening", StageType.ScreeningTest, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkflowDefinition.NotFound");
    }

    [Fact]
    public async Task Adds_the_stage_and_saves_when_the_workflow_exists()
    {
        var workflow = WorkflowDefinition.Create("Standard Flow");
        var repository = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns(workflow);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddStageToWorkflowCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new AddStageToWorkflowCommand(workflow.Id.Value, "Screening", StageType.ScreeningTest, 0), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        workflow.Stages.Should().ContainSingle(s => s.Name == "Screening");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Propagates_a_domain_rejection_without_saving()
    {
        var workflow = WorkflowDefinition.Create("Standard Flow");
        workflow.AddStage("Existing", StageType.ScreeningTest, 0);
        var repository = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns(workflow);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddStageToWorkflowCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new AddStageToWorkflowCommand(workflow.Id.Value, "Duplicate", StageType.ScreeningTest, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkflowDefinition.DuplicateStage");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
