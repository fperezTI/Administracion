# Modelo multiempresa

Un único tenant de Microsoft Entra ID, una sola instancia de la aplicación, una sola base de datos con
**separación lógica** mediante `CompanyId`. Hasta 50 empresas.

## Reglas

- Toda entidad transaccional (Asset, Movement, Assignment, MaintenanceOrder, Consumable, InternalRequest,
  Document, AuditEntry, …) tiene una columna `CompanyId` explícita, no nullable, indexada.
- **Nunca** se confía solamente en la empresa seleccionada en la interfaz. El backend:
  1. **[Implementado, F1]** Valida que el usuario tenga membresía (`UserCompany`) sobre la `CompanyId` de
     cada operación — cada handler de `Organization/OrgUnits` comprueba
     `currentCompany.AccessibleCompanyIds.Contains(request.CompanyId)` antes de leer o escribir.
  2. **[Implementado, F1]** Aplica un EF Core **Global Query Filter** sobre `OrgUnit`
     (`AppDbContext.OnModelCreating`: `HasQueryFilter(o =>
     companyContext.AccessibleCompanyIds.Contains(o.CompanyId))`) como defensa en profundidad — verificado
     por `MultiCompanyQueryFilterTests`, que confirma que ni siquiera una consulta sin el filtro explícito
     del handler puede ver filas de una empresa ajena. Cada bounded context añade este mismo filtro a sus
     propias entidades `CompanyId` al implementarse (Asset Registry, Inventory Operations, …).
  3. Los comandos de escritura validan explícitamente `CompanyId` en el handler antes de persistir.

### Resolución de la empresa activa (implementado en F1)

`ICurrentCompanyContext` (puerto en Application) se resuelve por
`HttpContextCurrentCompanyContext` (Infrastructure), poblado una sola vez por request por
`CurrentUserProvisioningMiddleware`:

1. Tras aprovisionar/actualizar el perfil local, el middleware obtiene las `CompanyId` del usuario
   (`ProvisionOrUpdateUserCommand` → `CurrentUserSnapshot.CompanyIds`) y las guarda como
   `AccessibleCompanyIds` en `HttpContext.Items`.
2. Si el request trae el encabezado `X-Active-Company-Id` **y** ese id está entre las `CompanyIds` del
   usuario, se guarda como `ActiveCompanyId`. Un id que el usuario no tiene concedido se ignora
   silenciosamente (el request continúa sin empresa activa, no falla) — el encabezado nunca se confía
   directamente, siempre se valida contra la membresía real leída de base de datos en esa misma request.
3. `GetMeQuery` expone `ActiveCompanyId` para que el frontend sepa qué empresa está activa; los módulos que
   operan explícitamente "sobre la empresa activa" (aún no hay ninguno en F1 — `OrgUnits` recibe la
   `CompanyId` como parámetro explícito de cada request, no implícita) la leerán de aquí cuando se
   construyan.

Verificado por `AuthenticationAndRbacTests.Active_company_header_is_only_honored_when_the_caller_is_actually_a_member`.

### Otras reglas

- El `CompanyId` de un `Asset` **no es editable directamente** por ningún endpoint (confirmado: ni
  `CreateAssetCommand` ni `UpdateAssetGeneralInfoCommand` lo aceptan como parámetro editable). Solo cambia
  como efecto transaccional de una `Transfer` entre empresas completada — **[Implementado, F5]**
  `Asset.CompleteCrossCompanyTransfer`, el único método que puede reasignarlo — a través del ciclo
  completo "aprobación (motor de F4) + salida + tránsito + recepción con firma" que ya se anticipaba aquí.
  `InternalFolio` se **regenera** en la secuencia de la empresa destino (un folio es propiedad de una
  empresa, no del activo — ver ADR 0007); lo que sí se preserva sin cambios es `AssetTag.Code` (global,
  ADR 0004) y todo el historial de `Movement`/`Assignment`/`Loan` previo, que nunca se borra ni se
  reescribe.
- Reportes: cada consulta de reporte soportará modo "por empresa" (filtro `CompanyId`) y "consolidado"
  (requiere permiso `Reports.ReadConsolidated`, ya que agrega datos de varias empresas y no toda persona con
  acceso a una empresa debe poder ver el consolidado) — módulo de reportes aún no construido (F10).

## Configuración por empresa

`CompanySettings` (1:1 con `Company`): nombre comercial/legal, identificación fiscal, logotipo (referencia a
`Document`), moneda base, zona horaria, formatos regionales, prefijos/secuencias de folio por tipo de
documento, plantillas activas por tipo, política de retención (`RetentionPolicy`) — pendiente de construir;
por ahora `Company` solo tiene los campos básicos usados en F1.

## Folios (implementado, F2)

`EfFolioGenerator` (`Infrastructure/Persistence/EfFolioGenerator.cs`) emite el siguiente folio con un único
`MERGE` atómico de SQL Server (upsert-and-increment sobre `organization.FolioSequences`, con `OUTPUT` del
nuevo valor) en vez de leer-modificar-escribir con reintento por concurrencia optimista — evita colisiones
bajo carga concurrente sin necesitar acceso al `ChangeTracker` de EF Core desde fuera del `DbContext`, y es
más simple de razonar que un `SEQUENCE` de SQL Server (que no es naturalmente particionable por
`CompanyId`). Formato: `{DocumentType}-{NextValue:D6}` (p. ej. `ASSET-000001`); un prefijo configurable por
empresa más allá del tipo de documento queda documentado como pendiente (ver supuesto §3.3 del análisis).
`IFolioGenerator` (puerto en Application) es la única forma en que un handler obtiene un folio — nunca se
genera inline.

**Importante — el código de etiqueta (`AssetTag.Code`) NO reutiliza el folio.** El folio es único solo por
empresa, pero el código de la etiqueta física (QR/código de barras) debe resolver sin ambigüedad a un único
activo en toda la base de datos — ver ADR 0004.

## Usuarios y empresas

Un usuario puede pertenecer a una o varias empresas (`UserCompany`). Los **roles y permisos son globales**
(ver `docs/security/authorization-rbac.md`) — la pertenencia a una empresa determina *sobre qué datos* puede
operar, no *qué acciones* tiene permitidas.
