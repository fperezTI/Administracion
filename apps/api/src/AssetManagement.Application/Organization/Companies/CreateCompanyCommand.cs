using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Organization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Organization.Companies;

/// <summary>Up to 50 companies (pedido §8) — the cap is a cross-aggregate policy, so it is checked
/// here rather than inside the Company aggregate itself.</summary>
public sealed record CreateCompanyCommand(
    string LegalName,
    string TradeName,
    string TaxId,
    string BaseCurrency,
    string TimeZone) : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Companies.Create;
}

public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TradeName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BaseCurrency).NotEmpty().Length(3);
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateCompanyCommandHandler(IApplicationDbContext db, IClock clock)
    : IRequestHandler<CreateCompanyCommand, Guid>
{
    private const int MaxCompanies = 50;

    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var currentCount = await db.Companies.CountAsync(cancellationToken);
        if (currentCount >= MaxCompanies)
        {
            throw new ConflictException($"No se pueden registrar más de {MaxCompanies} empresas.");
        }

        var company = Company.Create(
            request.LegalName, request.TradeName, request.TaxId, request.BaseCurrency, request.TimeZone, clock.UtcNow);

        db.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);

        return company.Id;
    }
}
