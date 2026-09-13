using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

public sealed class BehavioralIndicator : BaseEntity<Guid>
{
    public string Description { get; private set; } = string.Empty;

    private BehavioralIndicator()
    {
        // Reserved for EF Core materialization.
    }

    public BehavioralIndicator(Guid id, string description) : base(id)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A behavioral indicator must have a description.", nameof(description));

        Description = description;
    }
}
