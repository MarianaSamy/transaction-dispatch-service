namespace TransactionDispatch.Application.Options
{
    /// <summary>
    /// Bound from configuration: list of allowed file extensions (without leading dots).
    /// Default should include "xml".
    /// </summary>
    public class AllowedFileTypesOptions
    {
        public string[] AllowedFileTypes { get; set; } = new[] { "xml" };
    }
}
