using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrightPathAPI.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LessonsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LessonsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLessons()
        {
            var tutors = await _db.Tutors.ToListAsync();
            var lessons = await _db.Lessons.ToListAsync();

            return Ok(new
            {
                TotalTutors = tutors.Count,
                TotalLessons = lessons.Count,
                Tutors = tutors,
                Lessons = lessons
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateLesson([FromBody] Lesson req)
        {

            // RULE: Validate allowed status values
            if (req.Status != "booked" && req.Status != "cancelled" && req.Status != "no_show")
            {
                return BadRequest(new { error = "Invalid status. Allowed values are: booked, cancelled, no_show." });
            }

            // RULE: Centre closed on Mondays
            if (req.StartTime.DayOfWeek == DayOfWeek.Monday)
            {
                return BadRequest(new { error = "The centre is closed on Mondays for cleaning." });
            }

            // RULE: Teacher limit of 6 sessions per day
            var tutorLessons = await _db.Lessons
                .Where(l => l.TutorId == req.TutorId && l.Status != "cancelled")
                .ToListAsync();

            var tutorDailyCount = tutorLessons.Count(l =>
                    l.StartTime.Date == req.StartTime.Date
                );

            if (tutorDailyCount >= 6)
            {
                return BadRequest(new { error = "Tutor load exceeded: Cannot exceed 6 bookings in a single day." });
            }

            // RULE: Prevent schedule conflicts trong cùng ngày
            var newEndTime = req.StartTime.AddMinutes(req.DurationMin);

            var potentialConflicts = await _db.Lessons
                .Where(l => l.Status == "booked" && (l.RoomId == req.RoomId || l.TutorId == req.TutorId || l.StudentName == req.StudentName))
                .ToListAsync();

            var hasConflict = potentialConflicts.Any(l =>
                l.StartTime < newEndTime &&
                l.StartTime.AddMinutes(l.DurationMin) > req.StartTime
            );

            if (hasConflict)
            {
                return Conflict(new { error = "Schedule conflict: Room, Tutor, or Student is already booked for this timeslot." });
            }

            // Save the lesson if all rules are satisfied
            _db.Lessons.Add(req);
            await _db.SaveChangesAsync();

            return Created($"/api/v1/lessons/{req.Id}", req);
        }
    }
}