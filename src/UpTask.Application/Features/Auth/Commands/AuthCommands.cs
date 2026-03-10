using FluentValidation;
using MediatR;
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Entities;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;
using UpTask.Domain.ValueObjects;

namespace UpTask.Application.Features.Auth.Commands;

// ── DTOs ─────────────────────────────────────────────────────────────────────
public record AuthTokenDto(string AccessToken, string TokenType, int ExpiresIn, Guid UserId, string Email, string Role);
public record RegisterDto(string Name, string Email, string Password, string ConfirmPassword);
public record LoginDto(string Email, string Password);

// ── Register ──────────────────────────────────────────────────────────────────
public record RegisterCommand(string Name, string Email, string Password, string ConfirmPassword)
    : IRequest<AuthTokenDto>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain uppercase.")
            .Matches("[a-z]").WithMessage("Password must contain lowercase.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}

public class RegisterCommandHandler(
    IUserRepository userRepo,
    IUnitOfWork uow,
    IPasswordService passwordService,
    IJwtService jwtService) : IRequestHandler<RegisterCommand, AuthTokenDto>
{
    public async Task<AuthTokenDto> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (await userRepo.EmailExistsAsync(cmd.Email, ct))
            throw new BusinessRuleException("Email is already registered.");

        var email = new Email(cmd.Email);
        var hash = passwordService.Hash(cmd.Password);
        var user = User.Create(cmd.Name, email, hash);

        await userRepo.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        var token = jwtService.GenerateToken(user.Id, user.Email.Value, user.Profile.ToString());
        return new AuthTokenDto(token, "Bearer", 3600, user.Id, user.Email.Value, user.Profile.ToString());
    }
}

// ── Login ─────────────────────────────────────────────────────────────────────
public record LoginCommand(string Email, string Password) : IRequest<AuthTokenDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler(
    IUserRepository userRepo,
    IUnitOfWork uow,
    IPasswordService passwordService,
    IJwtService jwtService) : IRequestHandler<LoginCommand, AuthTokenDto>
{
    public async Task<AuthTokenDto> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByEmailAsync(cmd.Email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedException("Invalid credentials.");

        if (!user.IsActive()) throw new UnauthorizedException("Account is not active.");
        if (!passwordService.Verify(cmd.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials.");

        user.RecordLogin();
        uow.SaveChangesAsync(ct).GetAwaiter(); // fire-and-forget login time

        var token = jwtService.GenerateToken(user.Id, user.Email.Value, user.Profile.ToString());
        return new AuthTokenDto(token, "Bearer", 3600, user.Id, user.Email.Value, user.Profile.ToString());
    }
}

// ── Change Password ───────────────────────────────────────────────────────────
public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword, string ConfirmNewPassword)
    : IRequest<Unit>;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Must contain uppercase.")
            .Matches("[0-9]").WithMessage("Must contain digit.");
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public class ChangePasswordHandler(
    IUserRepository userRepo, IUnitOfWork uow, IPasswordService passwordService)
    : IRequestHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User", cmd.UserId);

        if (!passwordService.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");

        user.ChangePassword(passwordService.Hash(cmd.NewPassword));

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}