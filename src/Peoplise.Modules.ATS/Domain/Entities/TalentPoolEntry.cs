using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.Entities;

/// <summary>
/// Marks that a <c>CandidateProcess</c> has been tagged into the talent pool for
/// possible re-routing to other positions later, rather than being purely a
/// rejected/closed application.
/// </summary>
public sealed class TalentPoolEntry : BaseEntity<Guid>
{
    private readonly List<string> _tags = [];

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();
    public string? Note { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    private TalentPoolEntry()
    {
        // Reserved for EF Core materialization.
    }

    public TalentPoolEntry(Guid id, IEnumerable<string> tags, string? note, DateTimeOffset addedAt) : base(id)
    {
        _tags.AddRange(tags);
        Note = note;
        AddedAt = addedAt;
    }

    public void AddTag(string tag)
    {
        if (!_tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            _tags.Add(tag);
    }
}
