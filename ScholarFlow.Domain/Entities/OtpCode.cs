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
        ExpiresAt  = DateTime.UtcNow.AddMinutes(5),
        IsVerified = false,
    };

    public void MarkVerified() => IsVerified = true;
}
