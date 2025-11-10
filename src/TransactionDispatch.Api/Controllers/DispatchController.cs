using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TransactionDispatch.Application.DTOs;
using TransactionDispatch.Application.Ports;

namespace TransactionDispatch.Api.Controllers
{
    [ApiController]
    [Route("")]
    public class DispatchController : ControllerBase
    {
        private readonly IDispatchService _dispatchService;
        private readonly ILogger<DispatchController> _logger;

        public DispatchController(IDispatchService dispatchService, ILogger<DispatchController> logger)
        {
            _dispatchService = dispatchService ?? throw new ArgumentNullException(nameof(dispatchService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Start dispatching transaction files from the provided folder.
        /// Returns 202 Accepted and the JobId immediately.
        /// </summary>
        [HttpPost("dispatch-transactions")]
        [ProducesResponseType(typeof(object), 202)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> StartDispatch([FromBody] DispatchJobRequestDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var jobId = await _dispatchService.StartDispatchAsync(request, cancellationToken).ConfigureAwait(false);

                // Location header points to the status endpoint
                var statusUrl = Url.Action(nameof(GetStatus), values: new { jobId }) ?? $"/dispatch-status/{jobId}";
                Response.Headers["Location"] = statusUrl;

                return Accepted(new { jobId });
            }
            catch (ArgumentException aex)
            {
                _logger.LogWarning(aex, "Bad request to StartDispatch");
                return BadRequest(aex.Message);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499); // client closed / cancelled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting dispatch");
                return StatusCode(500, "Failed to start dispatch.");
            }
        }

        /// <summary>
        /// Get current status of a dispatch job.
        /// </summary>
        [HttpGet("dispatch-status/{jobId:guid}")]
        [ProducesResponseType(typeof(JobStatusDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetStatus([FromRoute] Guid jobId, CancellationToken cancellationToken)
        {
            if (jobId == Guid.Empty) return BadRequest("jobId is required.");

            try
            {
                var status = await _dispatchService.GetStatusAsync(jobId, cancellationToken).ConfigureAwait(false);
                if (status == null) return NotFound();
                return Ok(status);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job status for {JobId}", jobId);
                return StatusCode(500, "Failed to retrieve job status.");
            }
        }

        /// <summary>
        /// Simple health check endpoint.
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(200)]
        public IActionResult Health() => Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
}
