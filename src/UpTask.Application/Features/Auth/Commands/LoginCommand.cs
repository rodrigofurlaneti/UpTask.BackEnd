using FluentValidation;
using MediatR;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Features.Auth.DTOs;
using UpTask.Domain.Common;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Auth.Commands
{
    // ── Login ─────────────────────────────────────────────────────────────────────
    public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthTokenDto>>;

    public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public sealed class LoginCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IJwtService jwtService)
        : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
    {
        public async Task<Result<AuthTokenDto>> Handle(LoginCommand command, CancellationToken ct)
        {
            var user = await userRepository.GetByEmailAsync(command.Email.ToLowerInvariant(), ct);

            // Constant-time response: do not reveal whether email exists
            if (user is null || !user.IsActive())
                return Result.Failure<AuthTokenDto>(Error.Unauthorized("Invalid credentials."));

            if (!passwordService.Verify(command.Password, user.PasswordHash))
                return Result.Failure<AuthTokenDto>(Error.Unauthorized("Invalid credentials."));

            user.RecordLogin();
            await unitOfWork.SaveChangesAsync(ct); // fire-and-forget acceptable here

            var token = jwtService.GenerateToken(user.Id, user.Email.Value, user.Profile.ToString());

            return Result.Success(new AuthTokenDto(
                token, "Bearer", 3600,
                user.Id, user.Email.Value, user.Profile.ToString(), user.Name));
        }
    }
}
