using FluentAssertions;
using Peoplise.SharedKernel.Results;
using Xunit;

namespace Peoplise.SharedKernel.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_result_has_no_error_and_is_not_a_failure()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_result_carries_the_given_error()
    {
        var error = Error.NotFound("Position.NotFound", "Position was not found.");

        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void A_failure_must_carry_a_real_error_not_Error_None()
    {
        var act = () => Result.Failure(Error.None);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Generic_success_exposes_its_value()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Accessing_the_value_of_a_failed_generic_result_throws()
    {
        var result = Result.Failure<int>(Error.Conflict("X", "conflict"));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void A_value_implicitly_converts_to_a_successful_result()
    {
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }
}
