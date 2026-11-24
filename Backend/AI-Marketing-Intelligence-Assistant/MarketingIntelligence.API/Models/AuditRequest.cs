
using System.ComponentModel.DataAnnotations;

namespace MarketingIntelligence.API.Models
{
    public class AuditRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Website URL is required.")]
        [Url(ErrorMessage = "Please enter a valid website URL.")]
        public string Url { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? uri))
                yield break;

            if (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps)
            {
                yield return new ValidationResult(
                    "Only HTTP and HTTPS URLs are allowed.",
                    new[] { nameof(Url) });
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                yield return new ValidationResult(
                    "URLs containing username or password are not allowed.",
                    new[] { nameof(Url) });
            }
        }
    }
}
