using System.ComponentModel.DataAnnotations;

namespace TransactionDispatch.Application.DTOs
{
    /// <summary>
    /// Represents the input payload to trigger a new dispatch job.
    /// </summary>
    public class DispatchJobRequestDto
    {
        /// <summary>
        /// Path to the folder containing files to dispatch. Required.
        /// </summary>
        [Required(ErrorMessage = "FolderPath is required.")]
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Whether to delete files after successful Kafka dispatch.
        /// </summary>
        public bool DeleteAfterSend { get; set; } = false;
    }
}
