using MediatR;
using UpTask.Domain.Common; // Certifique-se de importar seu namespace de Result
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.TimeTracking.Commands;

// 1. Alteramos o retorno de Unit para Result
public record DeleteTimeEntryCommand(Guid Id, Guid UserId) : IRequest<Result>;

// 2. Ajustamos o Handler para implementar IRequestHandler para Result
public class DeleteTimeEntryHandler(ITimeEntryRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteTimeEntryCommand, Result>
{
    public async Task<Result> Handle(DeleteTimeEntryCommand cmd, CancellationToken ct)
    {
        var entry = await repo.GetByIdAsync(cmd.Id, ct);

        // Validação de existência e propriedade
        if (entry is null || entry.UserId != cmd.UserId)
        {
            return Result.Failure(Error.NotFound("TimeEntry.NotFound", "Registro não encontrado ou sem permissão."));
        }

        repo.Remove(entry);
        await uow.SaveChangesAsync(ct);

        // 3. Retornamos Result.Success() em vez de Unit.Value
        return Result.Success();
    }
}