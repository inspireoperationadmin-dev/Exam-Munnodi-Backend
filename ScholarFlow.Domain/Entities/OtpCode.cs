namespace ScholarFlow.Domain.Entities;

public sealed class OtpCode
{
    public Guid     Id         { get; private set; }
    public string   Email      { get; private set; } = string.Empty;
    public string   CodeHash   { get; private set; } = string.Empty;
    public DateTime CreatedAt  { get; private set; }
    public DateTime ExpiresAt  { get; private set; }
    public bool     IsVerified { get; private set; }

    private OtpCode() { }

    public static OtpCode Create(string email, string codeHash) => new()
    {
        Id         = Guid.NewGuid(),
        Email      = email.ToLowerInvariant(),
        CodeHash   = codeHash,
        CreatedAt  = DateTime.UtcNow,
        ExpiresAt  = DateTime.UtcNow.AddMinutes(10), // 10 min window to enter the code
        IsVerified = false,
    };
    
    public void MarkVerified()
    {
        IsVerified = true;
        ExpiresAt  = DateTime.UtcNow.AddMinutes(15); // grace window for next step
    }
}