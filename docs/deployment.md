# Despliegue

Ver `docs/architecture/decisions/0015-azure-cicd.md` para el razonamiento completo de cada decisión de
esta fase (F13).

**Estado real**: F13 terminó sin haber ejecutado ningún despliegue (sin suscripción de Azure ni
repositorio de GitHub remoto conectados en ese momento). Después sí se hizo un **primer despliegue real
contra Azure** (región `mexicocentral`) — a mano, siguiendo exactamente la sección "Primer despliegue
manual" de abajo (`az login` + `az deployment group create`), no a través de `.github/workflows/cd.yml`:
este repositorio local sigue sin ningún remoto de git configurado, así que el pipeline (que solo se
dispara con push a `main`) nunca se ha ejecutado. Ese despliegue manual encontró y corrigió errores reales
que quedaron reflejados directamente en el Bicep (ver el historial de `infrastructure/bicep/`):
`Standard_GRS` no es viable en `mexicocentral` (sin región pareada) y se usó `Standard_ZRS`; y, después de
bajar manualmente de costo en el portal de Azure (App Service Plan a `B1`, SQL a `Basic`, ACR a `Basic`,
Storage de `ZRS` a `LRS`), los valores por defecto de `main.bicep`/`sqlDatabase.bicep` se actualizaron para
que coincidan con lo que realmente está corriendo — si no, el siguiente despliegue habría revertido la
baja de costo en silencio, o directamente fallado (`Basic` no estaba en la lista `@allowed` de SKU de SQL).

No hay confirmación en el historial de que los pasos manuales de la sección 4 (usuario de base de datos
para la identidad administrada, secretos reales en Key Vault) ya se hayan completado — siguen siendo
necesarios si no se han hecho todavía.

## Arquitectura

- **Azure App Service (Linux, contenedores)** — un App Service para el API (.NET 9), uno para el
  frontend (Next.js) — las mismas imágenes que `infrastructure/docker/{api,web}.Dockerfile` ya construyen
  para Docker Compose local, nunca un artefacto de build distinto para producción.
- **Azure Container Registry** — una por ambiente (aislamiento entre `dev`/`test`/`prod`); las imágenes se
  construyen una sola vez (en `dev`) y se **promueven** sin reconstruir hacia `test` y `prod` vía
  `az acr import` — el mismo artefacto que pasó pruebas es el que llega a producción.
- **Azure SQL Database** — con **autenticación exclusiva de Microsoft Entra ID** (sin usuario/contraseña
  SQL en absoluto). La identidad administrada de cada App Service se otorga como usuario de la base de
  datos (paso manual, ver la guía abajo — no automatizable de forma confiable solo con Bicep).
- **Azure Storage Account** — contenedor blob privado `documents` (evidencias/documentos, F8) + cola
  `import-batches` (procesamiento asíncrono de importaciones, F9, ADR 0003). La cadena de conexión se
  guarda como secreto en Key Vault; los App Service la leen vía referencia `@Microsoft.KeyVault(...)`, no
  en texto plano.
- **Azure Key Vault** — con autorización RBAC (no *access policies* clásicas); cada App Service recibe el
  rol "Key Vault Secrets User" acotado al propio Key Vault.
- **Application Insights + Log Analytics** — telemetría (Serilog ya emite logs estructurados; ver
  `docs/architecture/overview.md`).
- **GitHub Actions con OIDC** — sin ningún secreto de Azure de larga vida en GitHub; cada *job* se
  autentica con una credencial federada por ambiente.

## Ambientes

`dev`, `test`, `prod` — cada uno como GitHub Environment con reglas de protección propias. `prod` requiere
aprobación manual de un revisor (configurado en GitHub, no en el *workflow* — ver `.github/workflows/cd.yml`).
Cada ambiente es su propio *resource group* de Azure, completamente aislado (su propio Key Vault, Storage
Account, SQL Server, Container Registry).

## Principios (sin cambios desde F0)

- HTTPS obligatorio en todos los ambientes (`httpsOnly: true` en cada App Service).
- Sin secretos en el repositorio ni en archivos de parámetros de Bicep — Key Vault + identidades
  administradas + referencias `@Microsoft.KeyVault(...)`.
- Configuración específica por ambiente vía App Settings (sin datos sensibles) + Key Vault (sensibles).
- Mínimo privilegio: cada identidad administrada solo tiene los roles que su servicio necesita
  (`AcrPull` acotado al registro, `Key Vault Secrets User` acotado al vault — nunca a nivel de suscripción).
- Sin Private Endpoints/VNet en V1 — ya documentado como *nice-to-have* no bloqueante desde F0 según
  costo/tiempo; punto de extensión futuro, no implementado aquí.

## Infraestructura como código

`infrastructure/bicep/main.bicep` (alcance *resource group*), orquesta los módulos de
`infrastructure/bicep/modules/`; un archivo de parámetros por ambiente en
`infrastructure/bicep/parameters/{dev,test,prod}.bicepparam` — cada uno con valores `CHANGE_ME` explícitos
donde se necesita información real (nunca un secreto). Validado localmente con `bicep build` (sintaxis) y
`bicep build-params` (contra `main.bicep`); no se pudo ejecutar `az deployment group validate`/`what-if`
reales sin una suscripción.

```bash
# Sintaxis (no requiere una suscripción de Azure)
bicep build infrastructure/bicep/main.bicep
bicep build-params infrastructure/bicep/parameters/dev.bicepparam

# Validación/despliegue real (requiere `az login` y una suscripción real)
az deployment group validate \
  --resource-group <tu-resource-group> \
  --template-file infrastructure/bicep/main.bicep \
  --parameters infrastructure/bicep/parameters/dev.bicepparam
```

## Guía paso a paso para el primer despliegue real

Esta es la secuencia que alguien con acceso real a Azure/GitHub debe seguir. La sección 3 (despliegue
manual) ya se ejecutó al menos para `prod` (ver "Estado real" arriba); no hay evidencia en el historial de
que las secciones 1, 2 y 4 se hayan completado (App Registration OIDC, Environments/secrets de GitHub,
usuario SQL para la identidad administrada, secretos reales en Key Vault) — revisar antes de asumir que ya
están hechas.

### 1. Azure — una vez por ambiente (`dev`, `test`, `prod`)

1. `az group create --name <rg-name> --location mexicocentral` (o la región elegida).
2. Crear un App Registration de Entra ID dedicado para OIDC (o reutilizar uno con credenciales
   federadas por ambiente) — **no** el mismo App Registration del login de usuarios
   (`docs/security/entra-id-setup.md`). Anotar su `Client Id`/`Tenant Id`.
3. Agregar una credencial federada a ese App Registration, *subject* exacto
   `repo:<org>/<repo>:environment:<dev|test|prod>` — esto es lo que le permite a GitHub Actions
   autenticarse sin secreto alguno.
4. Otorgarle al *Service Principal* de ese App Registration el rol `Contributor` sobre el *resource group*
   del ambiente (o un rol más acotado si se prefiere revisar permiso por permiso).
5. Decidir quién será el administrador de Entra ID de Azure SQL en este ambiente (una persona o un grupo
   de seguridad) y obtener su `object id` (`az ad user show --id <upn> --query id`) — va en
   `sqlAdminEntraObjectId`/`sqlAdminEntraLogin` del `.bicepparam` correspondiente.

### 2. GitHub — una vez por repositorio

1. Crear los tres *Environments* (`dev`, `test`, `prod`) en Settings → Environments. Agregar un revisor
   requerido **solo** en `prod`.
2. Por cada ambiente, configurar:
   - *Secrets*: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` (del paso 1.2).
   - *Variables*: `AZURE_RESOURCE_GROUP`, `AZURE_CONTAINER_REGISTRY_NAME` (el nombre real que Bicep generó
     tras el primer despliegue — ver el output `containerRegistryLoginServer`). `test` y `prod` además
     necesitan `DEV_AZURE_CONTAINER_REGISTRY_NAME`/`TEST_AZURE_CONTAINER_REGISTRY_NAME` respectivamente
     (el registro del ambiente anterior, de donde se promueve la imagen — ver `.github/workflows/cd.yml`).
3. Actualizar `infrastructure/bicep/parameters/{dev,test,prod}.bicepparam`, reemplazando cada `CHANGE_ME`.

### 3. Primer despliegue manual (antes de que exista una imagen que promover)

```bash
az login
az deployment group create \
  --resource-group <rg-name> \
  --template-file infrastructure/bicep/main.bicep \
  --parameters infrastructure/bicep/parameters/dev.bicepparam
```

Esto crea toda la infraestructura con `apiContainerImageTag`/`webContainerImageTag` en `latest` (que
todavía no existe en el registro — los App Service arrancarán en error hasta el primer *push* real, es
esperado). A partir de aquí, cada `git push` a `main` dispara `.github/workflows/cd.yml`, que construye,
promueve y despliega de verdad.

### 4. Pasos manuales que Bicep no automatiza (documentado, no un olvido)

- **Usuario de base de datos para cada identidad administrada** — Azure SQL con autenticación exclusiva de
  Entra ID requiere que la identidad administrada del App Service exista como usuario dentro de la propia
  base de datos. Ejecutar una sola vez, conectado como el administrador de Entra ID configurado en el paso
  1.5:
  ```sql
  CREATE USER [<nombre-del-app-service-api>] FROM EXTERNAL PROVIDER;
  ALTER ROLE db_datareader ADD MEMBER [<nombre-del-app-service-api>];
  ALTER ROLE db_datawriter ADD MEMBER [<nombre-del-app-service-api>];
  ALTER ROLE db_ddladmin ADD MEMBER [<nombre-del-app-service-api>]; -- para que EF Core aplique migraciones
  ```
- **Migraciones de EF Core** — este proyecto no incluyó (todavía) un paso de CI/CD que las aplique
  automáticamente; aplicarlas manualmente la primera vez (`dotnet ef database update` con la cadena de
  conexión real) o agregar un paso de despliegue dedicado como extensión futura.
- **Secretos reales en Key Vault** — `entra-web-client-secret` y `auth-secret` se crean con un valor
  *placeholder* (`REPLACE_ME_AFTER_DEPLOYMENT`) porque Bicep no puede generarlos. Reemplazarlos una vez:
  ```bash
  az keyvault secret set --vault-name <kv-name> --name entra-web-client-secret --value <valor-real>
  az keyvault secret set --vault-name <kv-name> --name auth-secret --value "$(node -e "console.log(require('crypto').randomBytes(32).toString('base64'))")"
  ```

## Pendiente (no bloqueante, fuera de alcance de V1)

- Automatizar la creación del usuario SQL vía `Microsoft.Resources/deploymentScripts` — se evaluó y se
  descartó para esta fase por ser un recurso genuinamente complejo (requiere su propia identidad con
  permisos de administrador SQL, una cuenta de Storage para logs) que no se pudo verificar contra una
  suscripción real.
- Aplicar migraciones de EF Core como parte de `cd.yml`.
- Private Endpoints/VNet, WAF de borde, protección DDoS administrada (ver `docs/security/hardening.md`).
