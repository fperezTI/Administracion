using AssetManagement.Domain.Assets;

namespace AssetManagement.Application.ImportExport;

/// <summary>
/// Validates one raw CSV row against the same rules <c>CreateAssetCommand</c> enforces (category active,
/// required custom fields, field lengths) plus import-only rules (serial-number duplicate detection,
/// within the batch and against the company's existing assets) — see ADR 0011 decision 3/4. Lookups are
/// pre-loaded once per batch by the caller (<see cref="ImportBatchProcessor"/>), not per row.
/// Case-insensitive on every code/enum column, since a CSV is hand-edited more often than a form field.
/// </summary>
public sealed class AssetImportRowValidator(
    IReadOnlyDictionary<string, AssetCategory> categoriesByCode,
    IReadOnlyDictionary<string, Guid> orgUnitIdsByCode,
    IReadOnlySet<string> existingSerialNumbers)
{
    private readonly HashSet<string> _serialsSeenInBatch = new(StringComparer.OrdinalIgnoreCase);

    public (ResolvedAssetImportRow? Row, IReadOnlyList<string> Errors) Validate(IReadOnlyDictionary<string, string> rawRow)
    {
        var errors = new List<string>();

        var categoryCode = GetValue(rawRow, AssetImportColumns.AssetCategoryCode);
        AssetCategory? category = null;
        if (string.IsNullOrWhiteSpace(categoryCode))
        {
            errors.Add($"'{AssetImportColumns.AssetCategoryCode}' es obligatorio.");
        }
        else if (!categoriesByCode.TryGetValue(categoryCode.Trim(), out category))
        {
            errors.Add($"No existe una categoría de activo con código '{categoryCode}'.");
        }
        else if (!category.IsActive)
        {
            errors.Add($"La categoría de activo '{categoryCode}' está desactivada.");
        }

        var brand = GetValue(rawRow, AssetImportColumns.Brand);
        if (string.IsNullOrWhiteSpace(brand))
        {
            errors.Add($"'{AssetImportColumns.Brand}' es obligatorio.");
        }
        else if (brand.Length > 100)
        {
            errors.Add($"'{AssetImportColumns.Brand}' no puede superar 100 caracteres.");
        }

        var model = GetValue(rawRow, AssetImportColumns.Model);
        if (string.IsNullOrWhiteSpace(model))
        {
            errors.Add($"'{AssetImportColumns.Model}' es obligatorio.");
        }
        else if (model.Length > 100)
        {
            errors.Add($"'{AssetImportColumns.Model}' no puede superar 100 caracteres.");
        }

        var serialNumber = GetValue(rawRow, AssetImportColumns.SerialNumber);
        if (!string.IsNullOrWhiteSpace(serialNumber))
        {
            serialNumber = serialNumber.Trim();
            if (serialNumber.Length > 100)
            {
                errors.Add($"'{AssetImportColumns.SerialNumber}' no puede superar 100 caracteres.");
            }
            else if (existingSerialNumbers.Contains(serialNumber))
            {
                errors.Add($"Ya existe un activo con el número de serie '{serialNumber}' en esta empresa.");
            }
            else if (!_serialsSeenInBatch.Add(serialNumber))
            {
                errors.Add($"El número de serie '{serialNumber}' está duplicado dentro del propio archivo.");
            }
        }
        else
        {
            serialNumber = null;
        }

        var description = GetValue(rawRow, AssetImportColumns.Description);
        if (!string.IsNullOrWhiteSpace(description) && description.Length > 500)
        {
            errors.Add($"'{AssetImportColumns.Description}' no puede superar 500 caracteres.");
        }

        var conditionRaw = GetValue(rawRow, AssetImportColumns.PhysicalCondition);
        PhysicalCondition physicalCondition = default;
        if (string.IsNullOrWhiteSpace(conditionRaw))
        {
            errors.Add($"'{AssetImportColumns.PhysicalCondition}' es obligatorio.");
        }
        else if (!Enum.TryParse(conditionRaw.Trim(), ignoreCase: true, out physicalCondition))
        {
            errors.Add(
                $"'{AssetImportColumns.PhysicalCondition}' inválido ('{conditionRaw}'). Valores válidos: {string.Join(", ", Enum.GetNames<PhysicalCondition>())}.");
        }

        var orgUnitCode = GetValue(rawRow, AssetImportColumns.OrgUnitCode);
        Guid? orgUnitId = null;
        if (!string.IsNullOrWhiteSpace(orgUnitCode))
        {
            if (orgUnitIdsByCode.TryGetValue(orgUnitCode.Trim(), out var resolvedOrgUnitId))
            {
                orgUnitId = resolvedOrgUnitId;
            }
            else
            {
                errors.Add($"No existe una ubicación (OrgUnit) con código '{orgUnitCode}' en esta empresa.");
            }
        }

        var identificationTechnologyRaw = GetValue(rawRow, AssetImportColumns.IdentificationTechnology);
        IdentificationTechnology? identificationTechnology = null;
        if (!string.IsNullOrWhiteSpace(identificationTechnologyRaw))
        {
            if (Enum.TryParse<IdentificationTechnology>(identificationTechnologyRaw.Trim(), ignoreCase: true, out var parsed))
            {
                identificationTechnology = parsed;
            }
            else
            {
                errors.Add(
                    $"'{AssetImportColumns.IdentificationTechnology}' inválido ('{identificationTechnologyRaw}'). " +
                    $"Valores válidos: {string.Join(", ", Enum.GetNames<IdentificationTechnology>())}.");
            }
        }

        var customFieldValues = new Dictionary<Guid, string>();
        if (category is not null)
        {
            var fieldsByCode = category.CustomFields.ToDictionary(f => f.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var (key, value) in rawRow)
            {
                if (!key.StartsWith(AssetImportColumns.CustomFieldPrefix, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var fieldCode = key[AssetImportColumns.CustomFieldPrefix.Length..];
                if (!fieldsByCode.TryGetValue(fieldCode, out var field))
                {
                    errors.Add($"El campo personalizado '{fieldCode}' no pertenece a la categoría '{category.Code}'.");
                    continue;
                }

                customFieldValues[field.Id] = value.Trim();
            }

            var missingRequired = category.CustomFields
                .Where(f => f.IsRequired && !customFieldValues.ContainsKey(f.Id))
                .Select(f => f.Name)
                .ToList();
            if (missingRequired.Count != 0)
            {
                errors.Add($"Faltan campos personalizados obligatorios: {string.Join(", ", missingRequired)}.");
            }
        }

        if (errors.Count != 0 || category is null)
        {
            return (null, errors);
        }

        var resolved = new ResolvedAssetImportRow(
            category.Id, brand!.Trim(), model!.Trim(), serialNumber, description?.Trim(), physicalCondition, orgUnitId,
            identificationTechnology, customFieldValues);

        return (resolved, errors);
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> rawRow, string column) =>
        rawRow.TryGetValue(column, out var value) ? value : null;
}
