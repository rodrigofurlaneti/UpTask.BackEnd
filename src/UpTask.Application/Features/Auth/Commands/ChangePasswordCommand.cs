using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Common;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Auth.Commands
{
    // ── Change Password ───────────────────────────────────────────────────────────
    public sealed record ChangePasswordCommand(
        Guid UserId,
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword) : IRequest<Result>;

    public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword)
                .NotEmpty().MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Must contain uppercase.")
                .Matches("[a-z]").WithMessage("Must contain lowercase.")
                .Matches("[0-9]").WithMessage("Must contain digit.");
            RuleFor(x => x.ConfirmNewPassword)
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
        }
    }

    public sealed class ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordService passwordService)
        : IRequestHandler<ChangePasswordCommand, Result>
    {
        public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken ct)
        {
            var user = await userRepository.GetByIdAsync(command.UserId, ct);

            if (user is null)
                return Result.Failure(Error.NotFound("User", command.UserId));

            if (!passwordService.Verify(command.CurrentPassword, user.PasswordHash))
                return Result.Failure(Error.Unauthorized("Current password is incorrect."));

            user.ChangePassword(passwordService.Hash(command.NewPassword));
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}
