using Bit.RustSDK;
using CommandDotNet;

namespace Bit.SeederUtility.Commands;

[Command("auth-hash", Description = "Derive the master password authentication hash for a seeded user")]
public class AuthHashCommand
{
    [DefaultCommand]
    public void Execute(AuthHashArgs args)
    {
        args.Validate();

        var keys = RustSdkService.GenerateUserKeys(args.Email, args.Password, args.KdfIterations);
        Console.WriteLine(keys.MasterPasswordHash);
    }
}
