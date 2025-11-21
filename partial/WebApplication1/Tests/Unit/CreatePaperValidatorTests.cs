using FluentValidation.TestHelper;
using WebApplication1.DTO.Request;
using WebApplication1.Validators;
using Xunit;

namespace WebApplication1.Tests.Unit;

/// <summary>
/// Unit tests for CreatePaperValidator
/// Tests validation rules for required fields, length constraints, and date validation
/// </summary>
public class CreatePaperValidatorTests
{
    private readonly CreatePaperValidator _validator;

    public CreatePaperValidatorTests()
    {
        _validator = new CreatePaperValidator();
    }

    #region Title Validation Tests

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "",
            Author: "John Doe",
            PublishedOn: DateTime.Today
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required and cannot be empty");
    }

    [Fact]
    public void Should_Have_Error_When_Title_Exceeds_MaxLength()
    {
        // Arrange
        var longTitle = new string('A', 201); // 201 characters
        var request = new CreatePaperRequest(
            Title: longTitle,
            Author: "John Doe",
            PublishedOn: DateTime.Today
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title must be at most 200 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Title_Is_Valid()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "Introduction to Machine Learning",
            Author: "John Doe",
            PublishedOn: DateTime.Today
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Title_Is_At_MaxLength()
    {
        // Arrange
        var titleAt200Chars = new string('A', 200); // Exactly 200 characters
        var request = new CreatePaperRequest(
            Title: titleAt200Chars,
            Author: "John Doe",
            PublishedOn: DateTime.Today
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    #endregion

    #region Author Validation Tests

    [Fact]
    public void Should_Have_Error_When_Author_Is_Empty()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "Deep Learning Fundamentals",
            Author: "",
            PublishedOn: DateTime.Today
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Author)
            .WithErrorMessage("Author is required and cannot be empty");
    }

    [Fact]
    public void Should_Have_Error_When_Author_Exceeds_MaxLength()
    {
        // Arrange
        var longAuthor = new string('B', 101); // 101 characters
        var request = new CreatePaperRequest(
            Title: "Deep Learning Fundamentals",
            Author: longAuthor,
            PublishedOn: DateTime.Today
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Author)
            .WithErrorMessage("Author name must be at most 100 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Author_Is_Valid()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "Deep Learning Fundamentals",
            Author: "Jane Smith",
            PublishedOn: DateTime.Today
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Author);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Author_Is_At_MaxLength()
    {
        // Arrange
        var authorAt100Chars = new string('B', 100); // Exactly 100 characters
        var request = new CreatePaperRequest(
            Title: "Deep Learning Fundamentals",
            Author: authorAt100Chars,
            PublishedOn: DateTime.Today
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Author);
    }

    #endregion

    #region PublishedOn Validation Tests

    [Fact]
    public void Should_Have_Error_When_PublishedOn_Is_In_Future()
    {
        // Arrange
        var futureDate = DateTime.Today.AddDays(1);
        var request = new CreatePaperRequest(
            Title: "Future Paper",
            Author: "Time Traveler",
            PublishedOn: futureDate
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PublishedOn)
            .WithErrorMessage("Published date cannot be in the future");
    }

    [Fact]
    public void Should_Not_Have_Error_When_PublishedOn_Is_Today()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "Today's Paper",
            Author: "Current Author",
            PublishedOn: DateTime.Today
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PublishedOn);
    }

    [Fact]
    public void Should_Not_Have_Error_When_PublishedOn_Is_In_Past()
    {
        // Arrange
        var pastDate = DateTime.Today.AddYears(-5);
        var request = new CreatePaperRequest(
            Title: "Historical Paper",
            Author: "Past Author",
            PublishedOn: pastDate
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PublishedOn);
    }

    #endregion

    #region Complete Request Validation Tests

    [Fact]
    public void Should_Pass_Validation_When_All_Fields_Are_Valid()
    {
        // Arrange
        var request = new CreatePaperRequest(
            Title: "Natural Language Processing in Practice",
            Author: "Robert Johnson",
            PublishedOn: new DateTime(2024, 1, 10)
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Multiple_Errors_When_Multiple_Fields_Are_Invalid()
    {
        // Arrange
        var futureDate = DateTime.Today.AddYears(1);
        var request = new CreatePaperRequest(
            Title: "",
            Author: "",
            PublishedOn: futureDate
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.Author);
        result.ShouldHaveValidationErrorFor(x => x.PublishedOn);
    }

    #endregion
}

