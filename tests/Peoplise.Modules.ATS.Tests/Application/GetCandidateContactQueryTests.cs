using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Peoplise.Infrastructure.Modules;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Application.Candidates.Queries;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.MultiTenancy;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Application;

public class GetCandidateContactQueryTests
{
    private static (AppDbContext Context, Guid TenantId, GetCandidateContactQueryHandler Handler) CreateHandler()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId.From(tenantId));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options, tenantContext, new ModuleAssemblyRegistry([typeof(CandidateProcess).Assembly]));

        return (context, tenantId, new GetCandidateContactQueryHandler(context));
    }

    private static CandidateProcess CreateProcess(Guid positionId, Guid candidateId, string email, string name, Guid firstStageId) =>
        CandidateProcess.Submit(
            CandidateId.From(candidateId), PositionId.From(positionId), WorkflowDefinitionId.New(),
            name, email, null, null, firstStageId, DateTimeOffset.UtcNow);

    [Fact]
    public async Task Returns_NotFound_when_no_application_exists_for_the_candidate_position_pair()
    {
        var (_, _, handler) = CreateHandler();

        var result = await handler.Handle(new GetCandidateContactQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Candidate.NotFound");
    }

    [Fact]
    public async Task Returns_the_candidate_s_email_and_name_when_an_application_exists()
    {
        var (context, tenantId, handler) = CreateHandler();
        var positionId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var process = CreateProcess(positionId, candidateId, "ada@example.com", "Ada Lovelace", Guid.NewGuid());
        process.TenantId = tenantId;
        context.Set<CandidateProcess>().Add(process);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new GetCandidateContactQuery(positionId, candidateId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("ada@example.com");
        result.Value.Name.Should().Be("Ada Lovelace");
    }

    [Fact]
    public async Task Does_not_match_a_candidate_from_a_different_position()
    {
        var (context, tenantId, handler) = CreateHandler();
        var candidateId = Guid.NewGuid();
        var process = CreateProcess(Guid.NewGuid(), candidateId, "ada@example.com", "Ada Lovelace", Guid.NewGuid());
        process.TenantId = tenantId;
        context.Set<CandidateProcess>().Add(process);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new GetCandidateContactQuery(Guid.NewGuid(), candidateId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Candidate.NotFound");
    }
}
