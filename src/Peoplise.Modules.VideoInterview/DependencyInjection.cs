using Microsoft.Extensions.DependencyInjection;
using Peoplise.Modules.VideoInterview.Application.Common;
using Peoplise.Modules.VideoInterview.Infrastructure.BackgroundJobs;

namespace Peoplise.Modules.VideoInterview;

/// <summary>
/// Registers what this module needs beyond the generic MediatR/FluentValidation/EF
/// discovery <c>Peoplise.Infrastructure.DependencyInjection.AddInfrastructure</c>
/// already does by scanning this module's assembly: the in-process transcription queue
/// and its background worker. Called explicitly from the Api composition root. The KVKK
/// retention sweep used to live here too — it moved to <c>Peoplise.Api.BackgroundJobs</c>
/// once it needed to cover HrBot's <c>Conversation</c> as well as this module's
/// <c>Case</c>; see ADR 0003.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddVideoInterviewModule(this IServiceCollection services)
    {
        services.AddSingleton<IVideoTranscriptionQueue, VideoTranscriptionQueue>();
        services.AddHostedService<VideoTranscriptionBackgroundService>();

        return services;
    }
}
