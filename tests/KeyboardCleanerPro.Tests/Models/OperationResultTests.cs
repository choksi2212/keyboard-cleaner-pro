using FluentAssertions;
using KeyboardCleanerPro.Core.Models;
using Xunit;

namespace KeyboardCleanerPro.Tests.Models;

/// <summary>Tests for the <see cref="OperationResult"/> and <see cref="OperationResult{T}"/> models.</summary>
public sealed class OperationResultTests
{
    // ── OperationResult (non-generic) ─────────────────────────────────────────

    [Fact]
    public void Success_IsSuccess_IsTrue()
    {
        var result = OperationResult.Success();
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ErrorCode.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Failure_WithMessage_IsSuccessFalse()
    {
        var result = OperationResult.Failure("Something went wrong");
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Something went wrong");
    }

    [Fact]
    public void Failure_WithErrorCode_SetsErrorCode()
    {
        var result = OperationResult.Failure("Access denied", errorCode: 5);
        result.ErrorCode.Should().Be(5);
    }

    [Fact]
    public void Failure_WithException_SetsException()
    {
        var ex     = new InvalidOperationException("test");
        var result = OperationResult.Failure("error", exception: ex);
        result.Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void Success_ToString_ContainsSuccess()
    {
        OperationResult.Success().ToString().Should().Contain("Success");
    }

    [Fact]
    public void Failure_ToString_ContainsFailure()
    {
        OperationResult.Failure("oops", 42).ToString().Should().Contain("Failure").And.Contain("oops").And.Contain("42");
    }

    // ── OperationResult{T} (generic) ──────────────────────────────────────────

    [Fact]
    public void GenericSuccess_ContainsValue()
    {
        var result = OperationResult<int>.Success(42);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_ValueIsDefault()
    {
        var result = OperationResult<int>.Failure("oops");
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(default);
    }

    [Fact]
    public void FromFailure_PropagatesErrorMessage()
    {
        var nonGeneric = OperationResult.Failure("propagated", 99);
        var generic    = OperationResult<string>.FromFailure(nonGeneric);

        generic.IsSuccess.Should().BeFalse();
        generic.ErrorMessage.Should().Be("propagated");
        generic.ErrorCode.Should().Be(99);
    }

    [Fact]
    public void GenericSuccess_ToString_ContainsValue()
    {
        OperationResult<string>.Success("hello").ToString().Should().Contain("hello");
    }
}
