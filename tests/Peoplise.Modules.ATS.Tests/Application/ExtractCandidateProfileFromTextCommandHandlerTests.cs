using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.SharedKernel.AI;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Application;

public class ExtractCandidateProfileFromTextCommandHandlerTests
{
    [Fact]
    public async Task Returns_whatever_the_AI_provider_extracted()
    {
        var aiProvider = Substitute.For<IAIProvider>();
        var extraction = new CandidateProfileExtraction("Ada Lovelace", "ada@example.com", null, null);
        aiProvider.ExtractCandidateProfileAsync("some pasted LinkedIn text", Arg.Any<CancellationToken>()).Returns(extraction);
        var handler = new ExtractCandidateProfileFromTextCommandHandler(aiProvider);

        var result = await handler.Handle(new ExtractCandidateProfileFromTextCommand("some pasted LinkedIn text"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(extraction);
    }

    [Fact]
    public async Task Passes_through_fields_the_provider_could_not_find_as_null()
    {
        var aiProvider = Substitute.For<IAIProvider>();
        var extraction = new CandidateProfileExtraction("Ada Lovelace", null, null, null);
        aiProvider.ExtractCandidateProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(extraction);
        var handler = new ExtractCandidateProfileFromTextCommandHandler(aiProvider);

        var result = await handler.Handle(new ExtractCandidateProfileFromTextCommand("just a name, nothing else"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().BeNull();
        result.Value.Phone.Should().BeNull();
        result.Value.ResumeUrl.Should().BeNull();
    }
}
