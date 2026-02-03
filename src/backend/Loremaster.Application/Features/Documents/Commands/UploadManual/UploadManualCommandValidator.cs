using FluentValidation;

namespace Loremaster.Application.Features.Documents.Commands.UploadManual;

/// <summary>
/// Validator for UploadManualCommand.
/// </summary>
public class UploadManualCommandValidator : AbstractValidator<UploadManualCommand>
{
    /// <summary>
    /// Maximum PDF file size (50 MB).
    /// </summary>
    private const int MaxFileSizeBytes = 50 * 1024 * 1024;
    
    /// <summary>
    /// PDF magic bytes header.
    /// </summary>
    private static readonly byte[] PdfMagicBytes = { 0x25, 0x50, 0x44, 0x46 }; // %PDF

    public UploadManualCommandValidator()
    {
        RuleFor(x => x.GameSystemId)
            .NotEmpty().WithMessage("Game system ID is required");

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Owner ID is required");

        RuleFor(x => x.PdfContent)
            .NotNull().WithMessage("PDF content is required")
            .NotEmpty().WithMessage("PDF content cannot be empty")
            .Must(BeValidSize).WithMessage($"PDF file size cannot exceed {MaxFileSizeBytes / 1024 / 1024} MB")
            .Must(BeValidPdf).WithMessage("File must be a valid PDF document");

        RuleFor(x => x.Title)
            .MaximumLength(500).WithMessage("Title cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Version)
            .MaximumLength(50).WithMessage("Version cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Version));
    }

    /// <summary>
    /// Validates that the file size is within limits.
    /// </summary>
    private static bool BeValidSize(byte[] content)
    {
        return content != null && content.Length <= MaxFileSizeBytes;
    }

    /// <summary>
    /// Validates that the content appears to be a PDF file.
    /// </summary>
    private static bool BeValidPdf(byte[] content)
    {
        if (content == null || content.Length < PdfMagicBytes.Length)
            return false;

        // Check PDF magic bytes
        for (var i = 0; i < PdfMagicBytes.Length; i++)
        {
            if (content[i] != PdfMagicBytes[i])
                return false;
        }

        return true;
    }
}
