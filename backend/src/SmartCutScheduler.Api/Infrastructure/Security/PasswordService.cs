using SmartCutScheduler.Api.Domain;
using Microsoft.AspNetCore.Identity;

namespace SmartCutScheduler.Api.Infrastructure.Security;

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string hashed, string password);
}

public class PasswordService(PasswordHasher<User> hasher) : IPasswordService
{
    public string Hash(User user, string password) => hasher.HashPassword(user, password);

    public bool Verify(User user, string hashed, string password) =>
        hasher.VerifyHashedPassword(user, hashed, password) is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
}
