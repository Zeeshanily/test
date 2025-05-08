// ---------------------
// Models
// ---------------------

public class User
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsLegitimate { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<UserOTP> OTPs { get; set; } = new();
}

public class UserOTP
{
    public int OTPId { get; set; }
    public int UserId { get; set; }
    public string OTPCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = default!;
}

// ---------------------
// DbContext
// ---------------------

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserOTP> UserOTPs => Set<UserOTP>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}

// ---------------------
// DTOs
// ---------------------

public class RequestAccessDto
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyOtpDto
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

// ---------------------
// Controller
// ---------------------

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;

    public AuthController(AppDbContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    [HttpPost("request-access")]
    public async Task<IActionResult> RequestAccess([FromBody] RequestAccessDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
        {
            user = new User { Email = dto.Email };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Notify admin (pseudo-code)
            await _emailSender.SendAsync("admin@example.com", "New access request", $"User {dto.Email} requested access.");
        }

        var otp = new UserOTP
        {
            UserId = user.UserId,
            OTPCode = new Random().Next(100000, 999999).ToString(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _db.UserOTPs.Add(otp);
        await _db.SaveChangesAsync();

        await _emailSender.SendAsync(user.Email, "Your OTP Code", $"Your OTP is: {otp.OTPCode}");

        return Ok(new { message = "OTP sent to your email." });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        var user = await _db.Users.Include(u => u.OTPs).FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return Unauthorized("User not found.");

        var latestOtp = user.OTPs.OrderByDescending(o => o.CreatedAt)
                                 .FirstOrDefault(o => !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);

        if (latestOtp == null || latestOtp.OTPCode != dto.Otp)
            return BadRequest("Invalid or expired OTP.");

        latestOtp.IsUsed = true;
        if (!user.IsLegitimate)
            user.IsLegitimate = true;

        await _db.SaveChangesAsync();

        // Generate token if needed (pseudo-code)
        // var token = _tokenService.GenerateToken(user);

        return Ok(new { message = "User verified successfully." });
    }
}

// ---------------------
// IEmailSender Interface (Simplified)
// ---------------------

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
