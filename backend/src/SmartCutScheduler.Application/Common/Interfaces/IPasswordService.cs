using SmartCutScheduler.Domain.Entities;

namespace SmartCutScheduler.Application.Common.Interfaces;

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string hashedPassword, string providedPassword);
}
