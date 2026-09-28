using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Application;

public class SetCandidateStatusCommandHandlerTests
{
    private static (IRepository<CandidateProcess, CandidateProcessId> Processes, IUnitOfWork UnitOfWork, SetCandidateStatusCommandHandler Handler)
        CreateHandler()
    {
        var processes = Substitute.For<IRepository<CandidateProcess, CandidateProcessId>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new SetCandidateStatusCommandHandler(processes, unitOfWork);
        return (processes, unitOfWork, handler);
    }

    private static CandidateProcess CreateProcess() =>
        CandidateProcess.Submit(
            CandidateId.New(), PositionId.New(), WorkflowDefinitionId.New(),
            "Ada Lovelace", "ada@example.com", null, null, Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public async Task Returns_NotFound_when_the_process_does_not_exist()
    {
        var (processes, _, handler) = CreateHandler();
        processes.GetByIdAsync(Arg.Any<CandidateProcessId>(), Arg.Any<CancellationToken>()).Returns((CandidateProcess?)null);

        var result = await handler.Handle(new SetCandidateStatusCommand(Guid.NewGuid(), PipelineStatus.UnderReview), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateProcess.NotFound");
    }

    [Fact]
    public async Task Moves_the_process_to_the_requested_status_and_saves()
    {
        var (processes, unitOfWork, handler) = CreateHandler();
        var process = CreateProcess();
        processes.GetByIdAsync(process.Id, Arg.Any<CancellationToken>()).Returns(process);

        var result = await handler.Handle(new SetCandidateStatusCommand(process.Id.Value, PipelineStatus.Interviewing), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        process.Status.Should().Be(PipelineStatus.Interviewing);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_Eliminated_as_a_manual_target_and_does_not_save()
    {
        var (processes, unitOfWork, handler) = CreateHandler();
        var process = CreateProcess();
        processes.GetByIdAsync(process.Id, Arg.Any<CancellationToken>()).Returns(process);

        var result = await handler.Handle(new SetCandidateStatusCommand(process.Id.Value, PipelineStatus.Eliminated), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateProcess.InvalidManualStatus");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
