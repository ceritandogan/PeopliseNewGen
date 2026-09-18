using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.Conversations.Queries;

/// <summary>
/// Not a business query — plumbing for the anonymous candidate-chat endpoints (no
/// session, so no tenant claim to resolve from). Mirrors ATS's
/// <c>ResolvePositionTenantQuery</c>: finds which tenant a conversation belongs to
/// *before* dispatching <c>ProcessUserResponseCommand</c>, so the API layer can set that
/// as the request's tenant override. Cross-tenant by design (<c>IgnoreQueryFilters</c>)
/// — the conversation id is the caller's only credential here.
/// </summary>
public sealed record ResolveConversationTenantQuery(Guid ConversationId) : IRequest<Result<Guid>>;

public sealed class ResolveConversationTenantQueryHandler : IRequestHandler<ResolveConversationTenantQuery, Result<Guid>>
{
    private readonly AppDbContext _context;

    public ResolveConversationTenantQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(ResolveConversationTenantQuery request, CancellationToken cancellationToken)
    {
        var conversationId = ConversationId.From(request.ConversationId);

        var tenantId = await _context.Set<Conversation>()
            .IgnoreQueryFilters()
            .Where(c => c.Id == conversationId)
            .Select(c => (Guid?)c.TenantId)
            .SingleOrDefaultAsync(cancellationToken);

        return tenantId is null
            ? Result.Failure<Guid>(Error.NotFound("Conversation.NotFound", $"No conversation '{request.ConversationId}' was found."))
            : Result.Success(tenantId.Value);
    }
}
