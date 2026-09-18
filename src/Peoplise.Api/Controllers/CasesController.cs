using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Modules.ATS.Application.Positions.Queries;
using Peoplise.Modules.VideoInterview.Application.Cases.Commands;
using Peoplise.Modules.VideoInterview.Application.Cases.Queries;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cases")]
public sealed class CasesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CasesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Anonymous, same reasoning as <see cref="ConversationsController"/>'s Start: a
    /// candidate taking the video interview has no session, so there's no tenant claim
    /// to resolve from. Resolves it from the position instead.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Start(StartCandidateCaseCommand command, CancellationToken cancellationToken)
    {
        var tenantResult = await _mediator.Send(new ResolvePositionTenantQuery(command.PositionId), cancellationToken);
        if (tenantResult.IsFailure)
            return tenantResult.ToActionResult(this);

        using var _ = AmbientTenantOverride.Begin(TenantId.From(tenantResult.Value));

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [AllowAnonymous]
    [HttpPost("{caseId:guid}/video-answers")]
    public async Task<IActionResult> SubmitVideoAnswer(
        Guid caseId, [FromForm] Guid stepId, IFormFile video, CancellationToken cancellationToken)
    {
        var tenantResult = await _mediator.Send(new ResolveCaseTenantQuery(caseId), cancellationToken);
        if (tenantResult.IsFailure)
            return tenantResult.ToActionResult(this);

        using var _ = AmbientTenantOverride.Begin(TenantId.From(tenantResult.Value));

        await using var stream = video.OpenReadStream();
        var command = new SubmitVideoAnswerCommand(caseId, stepId, stream, video.FileName, video.ContentType);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}
