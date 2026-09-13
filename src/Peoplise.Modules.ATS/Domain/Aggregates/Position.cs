using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Domain.Aggregates;

/// <summary>
/// A role being hired for. Owns its hiring team, position-specific custom fields, and
/// legal text; references a <see cref="WorkflowDefinition"/> (a separate aggregate —
/// definitions are reusable templates, not owned exclusively by one position) for the
/// stage sequence applicants go through.
/// </summary>
public sealed class Position : AggregateRoot<PositionId>, IHasTenant, IAuditableEntity
{
    private readonly List<PositionTeamMember> _teamMembers = [];
    private readonly List<CustomVariable> _customVariables = [];
    private readonly List<LegalDocument> _legalDocuments = [];

    public string Title { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public WorkMode WorkMode { get; private set; }
    public SeniorityLevel SeniorityLevel { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public WorkflowDefinitionId? WorkflowDefinitionId { get; private set; }

    public IReadOnlyCollection<PositionTeamMember> TeamMembers => _teamMembers.AsReadOnly();
    public IReadOnlyCollection<CustomVariable> CustomVariables => _customVariables.AsReadOnly();
    public IReadOnlyCollection<LegalDocument> LegalDocuments => _legalDocuments.AsReadOnly();

    // IHasTenant / IAuditableEntity — stamped by the infrastructure interceptors, never
    // set directly by domain code. Plain (not explicit-interface) properties: EF Core's
    // convention-based property discovery only picks up public properties, and an
    // explicit implementation would need per-property Fluent API configuration for no
    // real encapsulation benefit here.
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    private Position()
    {
        // Reserved for EF Core materialization.
    }

    private Position(
        PositionId id,
        string title,
        string department,
        string city,
        string country,
        WorkMode workMode,
        SeniorityLevel seniorityLevel,
        EmploymentType employmentType)
        : base(id)
    {
        Title = title;
        Department = department;
        City = city;
        Country = country;
        WorkMode = workMode;
        SeniorityLevel = seniorityLevel;
        EmploymentType = employmentType;
    }

    public static Position Create(
        string title,
        string department,
        string city,
        string country,
        WorkMode workMode,
        SeniorityLevel seniorityLevel,
        EmploymentType employmentType)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A position must have a title.", nameof(title));

        return new Position(PositionId.New(), title, department, city, country, workMode, seniorityLevel, employmentType);
    }

    public void AssignWorkflow(WorkflowDefinitionId workflowDefinitionId) =>
        WorkflowDefinitionId = workflowDefinitionId;

    /// <summary>
    /// Adds a user to this position's hiring team. Fails if the user is already a team
    /// member — re-adding with a different role must go through an explicit role change,
    /// not a silent overwrite, so this is a genuine business rule to test, not a bug.
    /// </summary>
    public Result AddTeamMember(string userId, PositionRole role)
    {
        if (_teamMembers.Any(m => m.UserId == userId))
            return Result.Failure(Error.Conflict("Position.TeamMemberAlreadyAdded", $"'{userId}' is already on this position's team."));

        _teamMembers.Add(new PositionTeamMember(Guid.NewGuid(), userId, role));
        return Result.Success();
    }

    public void AddCustomVariable(string key, string value) =>
        _customVariables.Add(new CustomVariable(Guid.NewGuid(), key, value));

    public void AddLegalDocument(string title, string content) =>
        _legalDocuments.Add(new LegalDocument(Guid.NewGuid(), title, content));
}
