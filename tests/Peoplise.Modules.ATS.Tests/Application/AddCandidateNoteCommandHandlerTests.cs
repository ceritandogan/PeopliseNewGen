using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Application;

public class AddCandidateNoteCommandHandlerTests
{
    private static CandidateProcess CreateProcess() =>
        CandidateProcess.Submit(
            CandidateId.New(), PositionId.New(), WorkflowDefinitionId.New(),
            "Ada Lovelace", "ada@example.com", null, null, Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public async Task Returns_NotFound_when_the_process_does_not_exist()
    {
        var processes = Substitute.For<IRepository<CandidateProcess, CandidateProcessId>>();
        processes.GetByIdAsync(Arg.Any<CandidateProcessId>(), Arg.Any<CancellationToken>()).Returns((CandidateProcess?)null);
        var handler = new AddCandidateNoteCommandHandler(processes, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddCandidateNoteCommand(Guid.NewGuid(), "reviewer-1", "Looks promising.", false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateProcess.NotFound");
    }

    [Fact]
    public async Task Adds_the_note_and_saves()
    {
        var process = CreateProcess();
        var processes = Substitute.For<IRepository<CandidateProcess, CandidateProcessId>>();
        processes.GetByIdAsync(process.Id, Arg.Any<CancellationToken>()).Returns(process);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddCandidateNoteCommandHandler(processes, unitOfWork);

        var result = await handler.Handle(
            new AddCandidateNoteCommand(process.Id.Value, "reviewer-1", "Looks promising.", IsPrivate: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        process.Notes.Should().ContainSingle(n => n.AuthorId == "reviewer-1" && n.Text == "Looks promising." && n.IsPrivate);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
