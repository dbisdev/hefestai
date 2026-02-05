using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Loremaster.Infrastructure.Persistence;

/// <summary>
/// Database seeder responsible for creating initial data required for the application.
/// Seeds an admin user on first run.
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;
    
    /// <summary>
    /// Default admin credentials. Should be changed after first login.
    /// </summary>
    private const string DefaultAdminEmail = "admin@loremaster.com";
    private const string DefaultAdminPassword = "Admin123!";
    private const string DefaultAdminDisplayName = "System Administrator";

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with initial data.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminUserAsync(cancellationToken);
    }

    /// <summary>
    /// Creates the default admin user if it doesn't exist.
    /// </summary>
    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var existingAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == DefaultAdminEmail && u.DeletedAt == null, cancellationToken);

        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin user already exists with email {Email}", DefaultAdminEmail);
            return;
        }

        // Hash password using BCrypt directly (same as PasswordHasher service)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DefaultAdminPassword, 12);

        var adminUser = User.Create(
            email: DefaultAdminEmail,
            passwordHash: passwordHash,
            displayName: DefaultAdminDisplayName,
            role: UserRole.Admin
        );

        await _context.Users.AddAsync(adminUser, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created default admin user: {Email} with password: {Password}. IMPORTANT: Change this password after first login!",
            DefaultAdminEmail,
            DefaultAdminPassword);
    }
}
