using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>One of a <see cref="Competency"/>'s 1–5 proficiency levels.</summary>
public sealed class CompetencyLevel : BaseEntity<Guid>
{
    public const int MinLevel = 1;
    public const int MaxLevel = 5;

    public int Level { get; private set; }
    public string Description { get; private set; } = string.Empty;

    private CompetencyLevel()
    {
        // Reserved for EF Core materialization.
    }

    public CompetencyLevel(Guid id, int level, string description) : base(id)
    {
        if (level is < MinLevel or > MaxLevel)
            throw new ArgumentOutOfRangeException(nameof(level), level, $"A competency level must be between {MinLevel} and {MaxLevel}.");

        Level = level;
        Description = description;
    }
}
