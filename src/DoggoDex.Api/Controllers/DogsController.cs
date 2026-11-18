using DoggoDex.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoggoDex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DogsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all seeded dog profiles from the database.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDogs()
    {
        var dogs = await _context.Dogs
            .Include(d => d.Owner)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Breed,
                d.Age,
                d.AggregateRating,
                d.ReviewCount,
                d.ProfilePhotoUrl,
                d.ShareableID,
                Owner = new
                {
                    d.Owner.Id,
                    d.Owner.Name,
                    d.Owner.Bio,
                    d.Owner.ProfilePhotoUrl,
                    d.Owner.AggregateRating
                }
            })
            .ToListAsync();

        return Ok(dogs);
    }
}