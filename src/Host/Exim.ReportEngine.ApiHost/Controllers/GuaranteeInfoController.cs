using Labeling.Application.GuaranteeInfo;
using Microsoft.AspNetCore.Mvc;

namespace Exim.ReportEngine.Controllers;

[ApiController]
[Route("api/guarantee-info")]
public sealed class GuaranteeInfoController : ControllerBase
{
    private readonly IGuaranteeInfoService _service;

    public GuaranteeInfoController(IGuaranteeInfoService service)
    {
        _service = service;
    }

    /// <summary>Gets all guarantee records.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GuaranteeInfoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var dtos = await _service.GetAllAsync(cancellationToken);
        return Ok(dtos);
    }

    /// <summary>Gets a guarantee record by its surrogate key.</summary>
    [HttpGet("{id:decimal}")]
    [ProducesResponseType<GuaranteeInfoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(decimal id, CancellationToken cancellationToken)
    {
        var dto = await _service.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Gets a guarantee record by letter number.</summary>
    [HttpGet("by-letter/{letterNo}")]
    [ProducesResponseType<GuaranteeInfoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByLetterNo(string letterNo, CancellationToken cancellationToken)
    {
        var dto = await _service.GetByLetterNoAsync(letterNo, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Gets all guarantee records for a customer.</summary>
    [HttpGet("by-customer/{customerId:decimal}")]
    [ProducesResponseType<IReadOnlyList<GuaranteeInfoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomerId(decimal customerId, CancellationToken cancellationToken)
    {
        var dtos = await _service.GetByCustomerIdAsync(customerId, cancellationToken);
        return Ok(dtos);
    }

    /// <summary>Creates a new guarantee record.</summary>
    [HttpPost]
    [ProducesResponseType<GuaranteeInfoDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateGuaranteeInfoRequest request, CancellationToken cancellationToken)
    {
        var dto = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.GrtIdpk }, dto);
    }

    /// <summary>Updates the general fields of an existing guarantee record.</summary>
    [HttpPut("{id:decimal}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(decimal id, [FromBody] UpdateGuaranteeInfoRequest request, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(id, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Deletes a guarantee record by its surrogate key.</summary>
    [HttpDelete("{id:decimal}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(decimal id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
