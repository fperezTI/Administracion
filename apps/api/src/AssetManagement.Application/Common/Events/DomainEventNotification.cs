using AssetManagement.Domain.SharedKernel;
using MediatR;

namespace AssetManagement.Application.Common.Events;

/// <summary>
/// Wraps a pure <see cref="IDomainEvent"/> (Domain has zero knowledge of MediatR) so
/// <c>AppDbContext.SaveChangesAsync</c> (Infrastructure) can publish it via <see cref="IPublisher"/> after
/// a successful save. A handler subscribes with <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c>.
/// </summary>
public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IDomainEvent;
