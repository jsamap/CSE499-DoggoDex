using DoggoDex.Api.Auth;
using DoggoDex.Api.Infrastructure;
using DoggoDex.Api.Models;
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

        if (await db.Establishments.AnyAsync(e => e.UserId == userId))
            return Conflict(new ProblemDetails { Title = "You already have an establishment profile." });

        var entity = new Establishment
        {
            UserId = userId,
            Name = request.Name.Trim(),
            PetPolicy = request.PetPolicy?.Trim(),
            PerksDescription = request.PerksDescription?.Trim(),
            ProfilePhotoUrl = request.ProfilePhotoUrl?.Trim()
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
            .FirstOrDefaultAsync(e => e.UserId == userId);

        return entity is null ? NotFound() : Ok(ToResponse(entity));
    }

    [HttpPut("me")]
    [Authorize(Policy = Policies.BusinessOnly)]
    public async Task<ActionResult<EstablishmentResponse>> UpdateMine(EstablishmentUpsertRequest request)
    {
        var userId = User.GetUserId();
        var entity = await db.Establishments.FirstOrDefaultAsync(e => e.UserId == userId);
        if (entity is null)
            return NotFound();

        entity.Name = request.Name.Trim();
        entity.PetPolicy = request.PetPolicy?.Trim();
        entity.PerksDescription = request.PerksDescription?.Trim();
        entity.ProfilePhotoUrl = request.ProfilePhotoUrl?.Trim();

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
        UserId = e.UserId,
        Name = e.Name,
        PetPolicy = e.PetPolicy,
        PerksDescription = e.PerksDescription,
        ProfilePhotoUrl = e.ProfilePhotoUrl
    };
}
