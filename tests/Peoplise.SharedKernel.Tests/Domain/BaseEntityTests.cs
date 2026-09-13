using FluentAssertions;
using Peoplise.SharedKernel.Domain;
using Xunit;

namespace Peoplise.SharedKernel.Tests.Domain;

public class BaseEntityTests
{
    private sealed class Widget : BaseEntity<Guid>
    {
        public Widget(Guid id) : base(id)
        {
        }
    }

    private sealed class OtherEntity : BaseEntity<Guid>
    {
        public OtherEntity(Guid id) : base(id)
        {
        }
    }

    [Fact]
    public void Entities_of_the_same_type_with_the_same_id_are_equal()
    {
        var id = Guid.NewGuid();

        var a = new Widget(id);
        var b = new Widget(id);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        var a = new Widget(Guid.NewGuid());
        var b = new Widget(Guid.NewGuid());

        a.Should().NotBe(b);
    }

    [Fact]
    public void Entities_of_different_types_sharing_an_id_are_not_equal()
    {
        var id = Guid.NewGuid();

        var widget = new Widget(id);
        var other = new OtherEntity(id);

        widget.Equals(other).Should().BeFalse();
    }
}
