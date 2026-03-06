using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Detyra3.Data;
using Detyra3.Entities;

namespace Detyra3.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EnrollmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Enrollment>>> GetEnrollments()
    {
        return await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Enrollment>> GetEnrollment(int id)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
        {
            return NotFound();
        }

        return enrollment;
    }

    [HttpPost]
    public async Task<ActionResult<Enrollment>> PostEnrollment(Enrollment enrollment)
    {
        var studentExists = await _context.Students.AnyAsync(s => s.Id == enrollment.StudentId);
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == enrollment.CourseId);

        if (!studentExists || !courseExists)
        {
            return BadRequest("Student or Course not found");
        }

        var existingEnrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == enrollment.StudentId && e.CourseId == enrollment.CourseId);

        if (existingEnrollment != null)
        {
            return Conflict("Student is already enrolled in this course");
        }

        enrollment.EnrollmentDate = DateTime.Now;
        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEnrollment), new { id = enrollment.Id }, enrollment);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutEnrollment(int id, Enrollment enrollment)
    {
        if (id != enrollment.Id)
        {
            return BadRequest();
        }

        _context.Entry(enrollment).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EnrollmentExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEnrollment(int id)
    {
        var enrollment = await _context.Enrollments.FindAsync(id);
        if (enrollment == null)
        {
            return NotFound();
        }

        _context.Enrollments.Remove(enrollment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool EnrollmentExists(int id)
    {
        return _context.Enrollments.Any(e => e.Id == id);
    }
}
