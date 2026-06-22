using CommandDotNet;

namespace Bit.SeederUtility.Commands;

public class AuthHashArgs : IArgumentModel
{
    [Option("email", Description = "Email for the user")]
    public string Email { get; set; } = null!;

    [Option("password", Description = "Password for the user")]
    public string Password { get; set; } = null!;

    [Option("kdf-iterations", Description = "KDF iteration count")]
    public int KdfIterations { get; set; } = 5_000;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new ArgumentException("Password is required.");
        }

        if (KdfIterations < 5_000)
        {
            throw new ArgumentException("KDF iterations must be at least 5,000.");
        }
    }
}
