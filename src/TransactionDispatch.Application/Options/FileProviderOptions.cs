// TransactionDispatch.Application.Options.FileProviderOptions.cs
namespace TransactionDispatch.Application.Options
{
    public class FileProviderOptions
    {
        /// <summary>
        /// Maximum allowed file size in bytes. Default = 200 KB.
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 200 * 1024;
    }
}
