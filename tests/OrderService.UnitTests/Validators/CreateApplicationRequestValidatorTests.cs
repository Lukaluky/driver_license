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
        var request = new CreateApplicationRequest("Иванов Иван Иванович", "B");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_For_Invalid_Category()
    {
        var request = new CreateApplicationRequest("Тест Тестов", "Z");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void Should_Fail_For_Empty_FullName()
    {
        var request = new CreateApplicationRequest("", "B");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }
}
