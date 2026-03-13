using MediatR; // Adicione este using

namespace UpTask.Domain.Common;

public interface IDomainEvent : INotification // Adicione a herança aqui
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}