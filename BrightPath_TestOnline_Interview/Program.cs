using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=brightpath.db"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// Database initialization and CSV seed data ingestion
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var baseDir = Directory.GetCurrentDirectory();
    var tutorPath = Path.Combine(baseDir, "tutors.csv");
    var lessonPath = Path.Combine(baseDir, "lessons_export.csv");

    // 1. Seed tutors.csv
    if (!db.Tutors.Any() && File.Exists(tutorPath))
    {
        var tutorLines = File.ReadAllLines(tutorPath).Skip(1);
        foreach (var line in tutorLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            db.Tutors.Add(new Tutor { 
                Id = cols[0].Trim(),
                Name = cols[1].Trim(),
                Subject = cols.Length > 2 ? cols[2].Trim() : string.Empty,
                Phone = cols.Length > 3 ? cols[3].Trim() : string.Empty
            });
        }
        db.SaveChanges();
    }

    // 2. Seed lessons_export.csv
    if (!db.Lessons.Any() && File.Exists(lessonPath))
    {
        var lessonLines = File.ReadAllLines(lessonPath).Skip(1);
        foreach (var line in lessonLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');

            int roomId = int.TryParse(cols[6].Trim().Replace("R", ""), out int r) ? r : 1;
            int duration = int.TryParse(cols[3].Trim(), out int d) ? d : 60;

            DateTimeOffset? cancelledAt = null;
            if (cols.Length > 8 && !string.IsNullOrWhiteSpace(cols[8]))
            {
                if (DateTimeOffset.TryParse(cols[8].Trim(), out DateTimeOffset parsedCancelledAt))
                {
                    cancelledAt = parsedCancelledAt;
                }
            }

            string? note = (cols.Length > 9 && !string.IsNullOrWhiteSpace(cols[9])) ? cols[9].Trim() : null;

            if (DateTimeOffset.TryParse($"{cols[1].Trim()}T{cols[2].Trim()}:00+07:00", out DateTimeOffset startDt))
            {
                db.Lessons.Add(new Lesson
                {
                    Id = Guid.NewGuid(),
                    LessonCode = cols[0].Trim(),
                    StudentName = cols[4].Trim(),
                    TutorId = cols[5].Trim(),
                    RoomId = roomId,
                    StartTime = startDt,
                    DurationMin = duration,
                    Status = cols[7].Trim(),
                    CancelledAt = cancelledAt,
                    Note = note
                });
            }
        }
        db.SaveChanges();
    }
}

app.Run();

// --- DATA MODELS ---

public class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? LessonCode { get; set; }

    [Required]
    public string TutorId { get; set; } = string.Empty;
    public string? StudentName { get; set; }

    [Range(1, 6)]
    public int RoomId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    [Range(60, 90)]
    public int DurationMin { get; set; }

    [RegularExpression("^(booked|cancelled|no_show)$", ErrorMessage = "Status must be either booked, cancelled, or no_show.")]
    public string Status { get; set; } = "booked";
    public DateTimeOffset? CancelledAt { get; set; }
    public string? Note { get; set; }
}

public class LessonRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LessonId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsAcknowledged { get; set; } = false;
}

public class Tutor
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonRevision> LessonRevisions => Set<LessonRevision>();
    public DbSet<Tutor> Tutors => Set<Tutor>();
}