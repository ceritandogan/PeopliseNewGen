using System.Reflection;

namespace Peoplise.Infrastructure.Modules;

/// <summary>
/// The set of module assemblies (Peoplise.Modules.ATS, .HrBot, .VideoInterview, …)
/// that own MediatR handlers and EF Core <c>IEntityTypeConfiguration&lt;T&gt;</c>
/// classes Infrastructure needs to discover at runtime.
/// </summary>
/// <remarks>
/// Infrastructure never references a module project directly — that would invert the
/// dependency direction a modular monolith depends on (modules depend on Infrastructure,
/// not the reverse). Instead, the Api composition root passes each module's assembly in
/// via <c>DependencyInjection.AddInfrastructure</c>, and this registry is what
/// <c>AppDbContext</c> and the MediatR/FluentValidation registration read to find those
/// assemblies via reflection (<c>ApplyConfigurationsFromAssembly</c>,
/// <c>RegisterServicesFromAssemblies</c>) without a compile-time reference.
/// </remarks>
public sealed class ModuleAssemblyRegistry
{
    public IReadOnlyList<Assembly> Assemblies { get; }

    public ModuleAssemblyRegistry(IEnumerable<Assembly> assemblies)
    {
        Assemblies = assemblies.Distinct().ToList();
    }
}
