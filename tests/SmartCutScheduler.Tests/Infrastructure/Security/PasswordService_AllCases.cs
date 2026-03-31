using Xunit;
using FluentAssertions;
using SmartCutScheduler.Infrastructure.Security;
using SmartCutScheduler.Domain.Entities;
using Microsoft.AspNetCore.Identity;

public class PasswordService_AllCases
{
    [Fact]
    public void Hash_ShouldReturnHash()
    {
        var hasher = new PasswordHasher<User>();
        var service = new PasswordService(hasher);
        var user = new User { Name = "Test" };
        var hash = service.Hash(user, "password");
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Verify_ShouldReturnTrue_ForCorrectPassword()
    {
        var hasher = new PasswordHasher<User>();
        var service = new PasswordService(hasher);
        var user = new User { Name = "Test" };
        var hash = service.Hash(user, "password");
        var result = service.Verify(user, hash, "password");
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_ForWrongPassword()
    {
        var hasher = new PasswordHasher<User>();
        var service = new PasswordService(hasher);
        var user = new User { Name = "Test" };
        var hash = service.Hash(user, "password");
        var result = service.Verify(user, hash, "wrong");
        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_ShouldReturnDifferentHashes_ForDifferentPasswords()
    {
        var hasher = new PasswordHasher<User>();
        var service = new PasswordService(hasher);
        var user = new User { Name = "Test" };
        var hash1 = service.Hash(user, "password1");
        var hash2 = service.Hash(user, "password2");
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_ShouldThrow_ForNullPassword()
    {
        var hasher = new PasswordHasher<User>();
        var service = new PasswordService(hasher);
        var user = new User { Name = "Test" };
        var hash = service.Hash(user, "password");
        Action act = () => service.Verify(user, hash, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Hash_ShouldNotThrow_ForEmptyPassword()
    {
        var hasher = new PasswordHasher<User>();
        var service = new PasswordService(hasher);
        var user = new User { Name = "Test" };
        var hash = service.Hash(user, "");
        hash.Should().NotBeNull();
    }
}
