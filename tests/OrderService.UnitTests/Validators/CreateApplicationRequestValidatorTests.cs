using FluentAssertions;
using FluentValidation.TestHelper;
using OrderService.Application.DTOs.Applications;
using OrderService.Application.Validators;
using Xunit;

namespace OrderService.UnitTests.Validators;

public class CreateApplicationRequestValidatorTests
{
    private readonly CreateApplicationRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_For_Valid_Request()
    {
        var request = new CreateApplicationRequest("900515300123", "Иванов Иван Иванович", "B");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("12345", "ФИО", "B")]
    [InlineData("abcdefghijkl", "ФИО", "B")]
    public void Should_Fail_For_Invalid_Iin(string iin, string name, string category)
    {
        var request = new CreateApplicationRequest(iin, name, category);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Iin);
    }

    [Fact]
    public void Should_Pass_When_Iin_Not_Provided()
    {
        var request = new CreateApplicationRequest(null, "Тест Тестов", "B");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Iin);
    }

    [Fact]
    public void Should_Pass_When_Over_18()
    {
        var request = new CreateApplicationRequest("900515300123", "Тест Тестов", "B");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Iin);
    }

    [Fact]
    public void Should_Fail_For_Invalid_Category()
    {
        var request = new CreateApplicationRequest("900515300123", "Тест Тестов", "Z");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void Should_Fail_For_Empty_FullName()
    {
        var request = new CreateApplicationRequest("900515300123", "", "B");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }
}
