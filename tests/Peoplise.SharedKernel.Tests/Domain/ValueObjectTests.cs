using FluentAssertions;
using Peoplise.SharedKernel.Domain;
using Xunit;

namespace Peoplise.SharedKernel.Tests.Domain;

public class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void Two_value_objects_with_the_same_components_are_equal()
    {
        var a = new Money(100m, "TRY");
        var b = new Money(100m, "TRY");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Value_objects_differing_in_any_component_are_not_equal()
    {
        var a = new Money(100m, "TRY");
        var b = new Money(100m, "USD");

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void A_value_object_is_never_equal_to_null()
    {
        var a = new Money(100m, "TRY");

        a.Equals(null).Should().BeFalse();
        (a == null).Should().BeFalse();
    }
}
