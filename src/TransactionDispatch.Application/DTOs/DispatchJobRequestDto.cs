using System.ComponentModel.DataAnnotations;

namespace TransactionDispatch.Application.DTOs
{
    public class DispatchJobRequestDto
    {
        [Required]
        public string FolderPath { get; set; } = string.Empty;

        public bool DeleteAfterSend { get; set; } = false;
    }
}
