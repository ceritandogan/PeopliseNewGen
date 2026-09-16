using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.ATS.Application.Workflows.Commands;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Application;

public class CreateWorkflowDefinitionCommandHandlerTests
{
    [Fact]
    public async Task Adds_the_new_workflow_and_saves()
    {
        var workflows = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new CreateWorkflowDefinitionCommandHandler(workflows, unitOfWork);

        var result = await handler.Handle(new CreateWorkflowDefinitionCommand("Standard Flow"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await workflows.Received(1).AddAsync(
            Arg.Is<WorkflowDefinition>(w => w.Name == "Standard Flow"), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
