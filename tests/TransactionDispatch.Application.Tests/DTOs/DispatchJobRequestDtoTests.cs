using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TransactionDispatch.Application.DTOs;
using Xunit;

namespace TransactionDispatch.Application.Tests.DTOs
{
    public class DispatchJobRequestDtoTests
    {
        [Fact]
        public void FolderPath_IsRequired_ShouldFailValidation_WhenEmpty()
        {
            // Arrange
            var dto = new DispatchJobRequestDto { FolderPath = "" };

            // Act
            var results = ValidateModel(dto);

            // Assert
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(DispatchJobRequestDto.FolderPath)));
            Assert.Contains(results, r => r.ErrorMessage!.Contains("FolderPath is required"));
        }

        [Fact]
        public void FolderPath_Valid_ShouldPassValidation()
        {
            // Arrange
            var dto = new DispatchJobRequestDto
            {
                FolderPath = "C:/data/input",
                DeleteAfterSend = true
            };

            // Act
            var results = ValidateModel(dto);

            // Assert
            Assert.Empty(results);
            Assert.Equal("C:/data/input", dto.FolderPath);
            Assert.True(dto.DeleteAfterSend);
        }

        // helper for attribute validation
        private static List<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }
    }
}
