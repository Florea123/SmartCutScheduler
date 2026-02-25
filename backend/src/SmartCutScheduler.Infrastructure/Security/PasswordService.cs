using Microsoft.AspNetCore.Identity;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Domain.Entities;

namespace SmartCutScheduler.Infrastructure.Security;

public class PasswordService(PasswordHasher<User> hasher) : IPasswordService
{
    public string Hash(User user, string password) => hasher.HashPassword(user, password);

    public bool Verify(User user, string hashedPassword, string providedPassword) =>
        hasher.VerifyHashedPassword(user, hashedPassword, providedPassword) is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
}
