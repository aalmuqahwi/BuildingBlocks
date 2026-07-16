using Xunit;

namespace BuildingBlocks.Domain.Tests;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ResultTests
{
    [Fact]
    public void Success_should_have_IsSuccess_true()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Value_should_return_input_when_result_is_success()
    {
        var result = Result<int>.Success(30);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value);
    }

    [Fact]
    public void Failure_should_have_IsFailure_true()
    {
        var result = Result.Failure(new("test.error", "this is test error"));

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Error_should_match_input()
    {
        var error = new Error("test.error", "this is test error");

        var result = Result.Failure(error);

        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Value_should_throw_when_result_is_failure()
    {
        var result = Result<int>.Failure(new Error("test.error", "this is test error"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member