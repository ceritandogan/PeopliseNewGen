using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.VideoInterview.Application.Cases.Commands;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Media;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Application;

public class SubmitVideoAnswerCommandHandlerTests
{
    private static CaseBotProject CreateProject(int retakesAllowed) =>
        CaseBotProject.Create("Backend Case Study", Guid.NewGuid(), retakesAllowed, retentionPeriodDays: 90).Value;

    [Fact]
    public async Task Uploads_the_video_and_records_it_on_the_case()
    {
        var project = CreateProject(retakesAllowed: 1);
        var @case = Case.Start(project.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var stepId = Guid.NewGuid();

        var cases = Substitute.For<IRepository<Case, CaseId>>();
        cases.GetByIdAsync(@case.Id, Arg.Any<CancellationToken>()).Returns(@case);
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("local-storage://video1.mp4");

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new SubmitVideoAnswerCommandHandler(cases, projects, fileStorage, unitOfWork);

        using var content = new MemoryStream([1, 2, 3]);
        var result = await handler.Handle(
            new SubmitVideoAnswerCommand(@case.Id.Value, stepId, content, "answer.mp4", "video/mp4"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        @case.StepConversations.Should().ContainSingle(c => c.VideoUrl == "local-storage://video1.mp4");
        await fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deletes_the_uploaded_file_when_the_retake_limit_is_exceeded()
    {
        var project = CreateProject(retakesAllowed: 0);
        var @case = Case.Start(project.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var stepId = Guid.NewGuid();
        @case.RecordVideoAnswer(stepId, "local-storage://first-take.mp4", retakesAllowed: 0, DateTimeOffset.UtcNow);

        var cases = Substitute.For<IRepository<Case, CaseId>>();
        cases.GetByIdAsync(@case.Id, Arg.Any<CancellationToken>()).Returns(@case);
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("local-storage://rejected-retake.mp4");

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new SubmitVideoAnswerCommandHandler(cases, projects, fileStorage, unitOfWork);

        using var content = new MemoryStream([1, 2, 3]);
        var result = await handler.Handle(
            new SubmitVideoAnswerCommand(@case.Id.Value, stepId, content, "retake.mp4", "video/mp4"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Case.RetakeLimitExceeded");
        await fileStorage.Received(1).DeleteAsync("local-storage://rejected-retake.mp4", Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
