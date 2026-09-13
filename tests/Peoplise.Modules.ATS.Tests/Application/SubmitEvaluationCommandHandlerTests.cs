using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.Events;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Application;

public class SubmitEvaluationCommandHandlerTests
{
    private static CandidateProcess CreateProcess(Guid firstStageId) =>
        CandidateProcess.Submit(
            CandidateId.New(), PositionId.New(), WorkflowDefinitionId.New(),
            "Ada Lovelace", "ada@example.com", null, null, firstStageId, DateTimeOffset.UtcNow);

    [Fact]
    public async Task Returns_NotFound_when_the_process_does_not_exist()
    {
        var repository = Substitute.For<IRepository<CandidateProcess, CandidateProcessId>>();
        repository.GetByIdAsync(Arg.Any<CandidateProcessId>(), Arg.Any<CancellationToken>())
            .Returns((CandidateProcess?)null);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new SubmitEvaluationCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new SubmitEvaluationCommand(Guid.NewGuid(), "evaluator-1", 80, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateProcess.NotFound");
    }

    [Fact]
    public async Task Adds_the_evaluation_raises_the_event_and_saves()
    {
        var process = CreateProcess(Guid.NewGuid());
        var repository = Substitute.For<IRepository<CandidateProcess, CandidateProcessId>>();
        repository.GetByIdAsync(Arg.Any<CandidateProcessId>(), Arg.Any<CancellationToken>()).Returns(process);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new SubmitEvaluationCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new SubmitEvaluationCommand(process.Id.Value, "evaluator-1", 85, "Strong technical answers."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        process.Evaluations.Should().ContainSingle(e => e.EvaluatorId == "evaluator-1" && e.Score.Value == 85);
        process.DomainEvents.Should().Contain(e => e is EvaluationSubmittedEvent);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Propagates_a_duplicate_evaluation_rejection_without_saving()
    {
        var process = CreateProcess(Guid.NewGuid());
        process.SubmitEvaluation("evaluator-1", EvaluationScore.From(80), null, DateTimeOffset.UtcNow);
        var repository = Substitute.For<IRepository<CandidateProcess, CandidateProcessId>>();
        repository.GetByIdAsync(Arg.Any<CandidateProcessId>(), Arg.Any<CancellationToken>()).Returns(process);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new SubmitEvaluationCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new SubmitEvaluationCommand(process.Id.Value, "evaluator-1", 90, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateProcess.DuplicateEvaluation");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
