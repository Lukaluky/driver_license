using System.Security.Claims;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs.Applications;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.Jobs;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
        => _applicationService = applicationService;

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [Authorize(Roles = "Applicant")]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest request)
    {
        try
        {
            var response = await _applicationService.CreateAsync(GetUserId(), request);

            BackgroundJob.Enqueue<ExternalCheckJob>(
                job => job.RunChecksAsync(response.Id));

            return CreatedAtAction(nameof(GetMy), new { }, response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Error = ex.Message });
        }
    }

    [HttpGet("my")]
    [Authorize(Roles = "Applicant")]
    public async Task<IActionResult> GetMy([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _applicationService.GetMyApplicationsAsync(GetUserId(), page, pageSize);
        return Ok(result);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Inspector")]
    public async Task<IActionResult> GetPending([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _applicationService.GetPendingApplicationsAsync(page, pageSize);
        return Ok(result);
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Inspector")]
    public async Task<IActionResult> Assign(Guid id)
    {
        try
        {
            var response = await _applicationService.AssignToInspectorAsync(id, GetUserId());
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("review")]
    [Authorize(Roles = "Inspector")]
    public async Task<IActionResult> Review([FromBody] ReviewRequest request)
    {
        try
        {
            var response = await _applicationService.ReviewAsync(GetUserId(), request);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/print")]
    [Authorize(Roles = "Inspector")]
    public async Task<IActionResult> Print(Guid id)
    {
        try
        {
            var response = await _applicationService.PrintLicenceAsync(id, GetUserId());
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}
