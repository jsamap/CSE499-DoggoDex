using DoggoDex.Api.Auth;
using DoggoDex.Api.Data;
using DoggoDex.Api.Domain;
using DoggoDex.Shared.Establishments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoggoDex.Api.Controllers;

[ApiController]
[Route("api/establishments")]
public class EstablishmentsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.BusinessOnly)]
    public async Task<ActionResult<EstablishmentResponse>> Create(EstablishmentUpsertRequest request)
    {
        var userId = User.GetUserId();

        if (await db.Establishments.AnyAsync(e => e.OwnerUserId == userId))
            return Conflict(new ProblemDetails { Title = "You already have an establishment profile." });

        var entity = new Establishment
        {
            OwnerUserId = userId,
            Name = request.Name.Trim(),
            Type = request.Type,
            Location = request.Location?.Trim(),
            Description = request.Description?.Trim()
        };

        db.Establishments.Add(entity);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToResponse(entity));
    }

    [HttpGet("me")]
    [Authorize(Policy = Policies.BusinessOnly)]
    public async Task<ActionResult<EstablishmentResponse>> GetMine()
    {
        var userId = User.GetUserId();
        var entity = await db.Establishments.AsNoTracking()
            .FirstOrDefaultAsync(e => e.OwnerUserId == userId);

        return entity is null ? NotFound() : Ok(ToResponse(entity));
    }

    [HttpPut("me")]
    [Authorize(Policy = Policies.BusinessOnly)]
    public async Task<ActionResult<EstablishmentResponse>> UpdateMine(EstablishmentUpsertRequest request)
    {
        var userId = User.GetUserId();
        var entity = await db.Establishments.FirstOrDefaultAsync(e => e.OwnerUserId == userId);
        if (entity is null)
            return NotFound();

        entity.Name = request.Name.Trim();
        entity.Type = request.Type;
        entity.Location = request.Location?.Trim();
        entity.Description = request.Description?.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToResponse(entity));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<EstablishmentResponse>> GetById(Guid id)
    {
        var entity = await db.Establishments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return entity is null ? NotFound() : Ok(ToResponse(entity));
    }

    private static EstablishmentResponse ToResponse(Establishment e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Type = e.Type,
        Location = e.Location,
        Description = e.Description,
        CreatedAtUtc = e.CreatedAtUtc
    };
}