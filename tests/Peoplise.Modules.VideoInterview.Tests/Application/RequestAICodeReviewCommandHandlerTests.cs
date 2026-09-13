using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.VideoInterview.Application.Cases.Commands;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.AI;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Application;

public class RequestAICodeReviewCommandHandlerTests
{
    private static Case CreateCase() =>
        Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public async Task Returns_NotFound_when_the_case_does_not_exist()
    {
        var cases = Substitute.For<IRepository<Case, CaseId>>();
        cases.GetByIdAsync(Arg.Any<CaseId>(), Arg.Any<CancellationToken>()).Returns((Case?)null);
        var aiProvider = Substitute.For<IAIProvider>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new RequestAICodeReviewCommandHandler(cases, aiProvider, unitOfWork);

        var result = await handler.Handle(
            new RequestAICodeReviewCommand(Guid.NewGuid(), Guid.NewGuid(), "Reverse a string", "def f(): pass"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Case.NotFound");
    }

    [Fact]
    public async Task Records_the_mocked_AI_reviews_5_dimension_score_on_the_case_and_returns_it()
    {
        var @case = CreateCase();
        var stepId = Guid.NewGuid();
        var cases = Substitute.For<IRepository<Case, CaseId>>();
        cases.GetByIdAsync(@case.Id, Arg.Any<CancellationToken>()).Returns(@case);

        var mockedReview = new CodeReviewResult(Readability: 18, Functionality: 20, DataValidation: 15, UseCaseHandling: 17, Syntax: 19);
        var aiProvider = Substitute.For<IAIProvider>();
        aiProvider.ReviewCodeAsync("Reverse a string", "def reverse(s): return s[::-1]", Arg.Any<CancellationToken>())
            .Returns(mockedReview);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new RequestAICodeReviewCommandHandler(cases, aiProvider, unitOfWork);

        var result = await handler.Handle(
            new RequestAICodeReviewCommand(@case.Id.Value, stepId, "Reverse a string", "def reverse(s): return s[::-1]"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(mockedReview);
        result.Value.Total.Should().Be(89); // 18+20+15+17+19

        @case.CodeReviews.Should().ContainSingle(r =>
            r.StepId == stepId && r.Readability == 18 && r.Functionality == 20
            && r.DataValidation == 15 && r.UseCaseHandling == 17 && r.Syntax == 19);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_perfect_score_across_all_five_dimensions_totals_100()
    {
        var @case = CreateCase();
        var cases = Substitute.For<IRepository<Case, CaseId>>();
        cases.GetByIdAsync(@case.Id, Arg.Any<CancellationToken>()).Returns(@case);

        var aiProvider = Substitute.For<IAIProvider>();
        aiProvider.ReviewCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CodeReviewResult(20, 20, 20, 20, 20));

        var handler = new RequestAICodeReviewCommandHandler(cases, aiProvider, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new RequestAICodeReviewCommand(@case.Id.Value, Guid.NewGuid(), "Question", "perfect code"), CancellationToken.None);

        result.Value.Total.Should().Be(100);
    }
}
