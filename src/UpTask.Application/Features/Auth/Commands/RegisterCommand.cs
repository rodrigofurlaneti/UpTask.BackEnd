using FluentValidation;
using MediatR;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Features.Auth.DTOs;
using UpTask.Domain.Common;
using UpTask.Domain.Entities;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;
using UpTask.Domain.ValueObjects;

namespace UpTask.Application.Features.Auth.Commands
{
    // ── Register ──────────────────────────────────────────────────────────────────
    public sealed record RegisterCommand(
        string Name,
        string Email,
        string Password,
        string ConfirmPassword) : IRequest<Result<AuthTokenDto>>;

    public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MinimumLength(2).WithMessage("Name must have at least 2 characters.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must have at least 8 characters.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords do not match.");
        }
    }

    public sealed class RegisterCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IJwtService jwtService)
        : IRequestHandler<RegisterCommand, Result<AuthTokenDto>>
    {
        public async Task<Result<AuthTokenDto>> Handle(RegisterCommand command, CancellationToken ct)
        {
            if (await userRepository.EmailExistsAsync(command.Email, ct))
                return Result.Failure<AuthTokenDto>(
                    Error.Conflict("User", "This email is already registered."));

            Email email;
            try
            {
                email = new Email(command.Email);
            }
            catch (DomainException ex)
            {
                return Result.Failure<AuthTokenDto>(Error.Validation("Email", ex.Message));
            }

            var hash = passwordService.Hash(command.Password);
            var user = User.Create(command.Name, email, hash);

            await userRepository.AddAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);

            var token = jwtService.GenerateToken(user.Id, user.Email.Value, user.Profile.ToString());

            return Result.Success(new AuthTokenDto(
                token, "Bearer", 3600,
                user.Id, user.Email.Value, user.Profile.ToString(), user.Name));
        }
    }
}
