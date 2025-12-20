using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/site")]
public class SiteContentController : ControllerBase
{
    private readonly AppDbContext _db;

    public SiteContentController(AppDbContext db) => _db = db;

    // Public: Get all site data for frontend
    [HttpGet]
    public async Task<ActionResult<SiteDataResponse>> GetSiteData()
    {
        var services = await _db.Services.Where(s => s.IsActive).OrderBy(s => s.SortOrder).ToListAsync();
        var blocks = await _db.SiteBlocks.OrderBy(b => b.SortOrder).ToListAsync();
        var testimonials = await _db.Testimonials.Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToListAsync();
        var faq = await _db.FaqItems.Where(f => f.IsActive).OrderBy(f => f.SortOrder).ToListAsync();
        var showcase = await _db.ShowcaseItems.Where(s => s.IsActive).OrderBy(s => s.SortOrder).ToListAsync();
        var content = await _db.SiteContents.ToDictionaryAsync(c => c.Key, c => c.Value);

        return Ok(new SiteDataResponse(services, blocks, testimonials, faq, showcase, content));
    }

    // Blocks management
    [HttpGet("blocks")]
    [Authorize]
    public async Task<ActionResult<List<SiteBlock>>> GetBlocks()
    {
        return await _db.SiteBlocks.OrderBy(b => b.SortOrder).ToListAsync();
    }

    [HttpPut("blocks/{id}")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> UpdateBlock(int id, SiteBlock block)
    {
        if (id != block.Id) return BadRequest();
        _db.Entry(block).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("blocks/reorder")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> ReorderBlocks([FromBody] List<int> blockIds)
    {
        for (int i = 0; i < blockIds.Count; i++)
        {
            var block = await _db.SiteBlocks.FindAsync(blockIds[i]);
            if (block != null) block.SortOrder = i;
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Testimonials
    [HttpGet("testimonials")]
    [Authorize]
    public async Task<ActionResult<List<Testimonial>>> GetTestimonials()
    {
        return await _db.Testimonials.OrderBy(t => t.SortOrder).ToListAsync();
    }

    [HttpPost("testimonials")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<ActionResult<Testimonial>> CreateTestimonial(Testimonial testimonial)
    {
        _db.Testimonials.Add(testimonial);
        await _db.SaveChangesAsync();
        return Ok(testimonial);
    }

    [HttpPut("testimonials/{id}")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> UpdateTestimonial(int id, Testimonial testimonial)
    {
        if (id != testimonial.Id) return BadRequest();
        _db.Entry(testimonial).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("testimonials/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTestimonial(int id)
    {
        var item = await _db.Testimonials.FindAsync(id);
        if (item == null) return NotFound();
        _db.Testimonials.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // FAQ
    [HttpGet("faq")]
    [Authorize]
    public async Task<ActionResult<List<FaqItem>>> GetFaq()
    {
        return await _db.FaqItems.OrderBy(f => f.SortOrder).ToListAsync();
    }

    [HttpPost("faq")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<ActionResult<FaqItem>> CreateFaq(FaqItem faq)
    {
        _db.FaqItems.Add(faq);
        await _db.SaveChangesAsync();
        return Ok(faq);
    }

    [HttpPut("faq/{id}")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> UpdateFaq(int id, FaqItem faq)
    {
        if (id != faq.Id) return BadRequest();
        _db.Entry(faq).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("faq/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteFaq(int id)
    {
        var item = await _db.FaqItems.FindAsync(id);
        if (item == null) return NotFound();
        _db.FaqItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Content key-value pairs
    [HttpPut("content")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> UpdateContent([FromBody] Dictionary<string, string> content)
    {
        foreach (var (key, value) in content)
        {
            var existing = await _db.SiteContents.FirstOrDefaultAsync(c => c.Key == key);
            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.SiteContents.Add(new SiteContent { Key = key, Value = value });
            }
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record SiteDataResponse(
    List<Service> Services,
    List<SiteBlock> Blocks,
    List<Testimonial> Testimonials,
    List<FaqItem> Faq,
    List<ShowcaseItem> Showcase,
    Dictionary<string, string> Content
);
