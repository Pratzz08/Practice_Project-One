using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS so React can talk to the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors();

// API 1: Save Data (POST)
app.MapPost("/api/users", async (User user, AppDbContext db) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/api/users/{user.Id}", user);
});

// API 2: Fetch Data (GET)
app.MapGet("/api/users", async (AppDbContext db) =>
{
    return await db.Users.OrderByDescending(u => u.Id).ToListAsync();
});

// Extra: Update Data (PUT)
app.MapPut("/api/users/{id}", async (int id, User updatedUser, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    user.Age = updatedUser.Age;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Extra: Delete Data (DELETE)
app.MapDelete("/api/users/{id}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

// --- Models and Context ---
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    // This overrides EF Core default naming conventions to target your exact PostgreSQL setup
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Forces EF Core to use the lowercase table and property names matching your DB
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<User>().Property(u => u.Id).HasColumnName("id");
        modelBuilder.Entity<User>().Property(u => u.Name).HasColumnName("name");
        modelBuilder.Entity<User>().Property(u => u.Email).HasColumnName("email");
        modelBuilder.Entity<User>().Property(u => u.Age).HasColumnName("age");
    }
}