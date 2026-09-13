using Microsoft.Extensions.DependencyInjection;
using Peoplise.Modules.VideoInterview.Application.Common;
using Peoplise.Modules.VideoInterview.Infrastructure.BackgroundJobs;

namespace Peoplise.Modules.VideoInterview;

/// <summary>
/// Registers what this module needs beyond the generic MediatR/FluentValidation/EF
/// discovery <c>Peoplise.Infrastructure.DependencyInjection.AddInfrastructure</c>
/// already does by scanning this module's assembly: the in-process transcription queue
/// and its background worker. Called explicitly from the Api composition root.
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
