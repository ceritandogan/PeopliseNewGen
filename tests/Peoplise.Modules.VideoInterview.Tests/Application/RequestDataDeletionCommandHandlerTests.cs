using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.VideoInterview.Application.Cases.Commands;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Media;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Application;

public class RequestDataDeletionCommandHandlerTests
{
    [Fact]
    public async Task Deletes_every_referenced_file_before_anonymizing_on_consent_withdrawal()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        @case.RecordVideoAnswer(Guid.NewGuid(), "local-storage://video.mp4", retakesAllowed: 0, DateTimeOffset.UtcNow);
        @case.RecordDocumentUpload(Guid.NewGuid(), "local-storage://doc.pdf", DateTimeOffset.UtcNow);

        var cases = Substitute.For<IRepository<Case, CaseId>>();
        cases.GetByIdAsync(@case.Id, Arg.Any<CancellationToken>()).Returns(@case);
        var fileStorage = Substitute.For<IFileStorageService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new RequestDataDeletionCommandHandler(cases, fileStorage, unitOfWork);

        var result = await handler.Handle(
            new RequestDataDeletionCommand(@case.Id.Value, DataDeletionReason.ConsentWithdrawn, "No longer interested."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await fileStorage.Received(1).DeleteAsync("local-storage://video.mp4", Arg.Any<CancellationToken>());
        await fileStorage.Received(1).DeleteAsync("local-storage://doc.pdf", Arg.Any<CancellationToken>());
        @case.CandidateId.Should().Be(Guid.Empty);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Uses_ExpireRetention_for_the_RetentionExpired_reason()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var cases = Substitute.For<IRepository<Case, CaseId>>();
        cases.GetByIdAsync(@case.Id, Arg.Any<CancellationToken>()).Returns(@case);
        var handler = new RequestDataDeletionCommandHandler(cases, Substitute.For<IFileStorageService>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new RequestDataDeletionCommand(@case.Id.Value, DataDeletionReason.RetentionExpired, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        @case.Status.Should().Be(CaseStatus.RetentionExpired);
    }
}
