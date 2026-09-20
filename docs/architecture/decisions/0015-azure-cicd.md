# ADR 0015 — F13: infraestructura Azure y CI/CD, sin un despliegue real posible en esta sesión

## Estado
Aceptado (F13, 2026-09-20).

## Contexto
El propio roadmap ya lo advertía desde el análisis inicial: *"F13 — Azure/CI-CD productivo | Bicep,
GitHub Actions con OIDC, ambientes dev/test/prod | **Requiere información de Azure (§18)**"*. La sección
18 lista exactamente lo que nunca se recibió en esta conversación: suscripción de Azure, resource
group/región, SKUs, organización/nombre del repositorio de GitHub (`git remote -v` no devuelve nada — no
hay repositorio remoto conectado, ni un solo commit todavía), y credenciales reales de un App Registration
de Entra ID para *login* federado.

`docs/deployment.md` (escrito en F0) ya anticipaba exactamente esto: *"La infraestructura como código
(Bicep) se construye parametrizada para no bloquear el desarrollo del código de aplicación"*. F13 entrega
esa infraestructura y los flujos de CI/CD completos y correctos — verificados con las herramientas de
sintaxis que sí se pudieron obtener en este entorno (CLI de Bicep standalone, `actionlint`) — sin un
despliegue real ni una ejecución real de GitHub Actions, honestamente documentado así.

## Decisiones

### 1. App Service (Linux) para contenedores, reutilizando las imágenes ya existentes
`docs/deployment.md` dejó abierto desde F0 si el frontend se serviría "en modo Node/Standalone o App
Service para contenedores según se confirme en F13". Se eligió contenedores — las mismas imágenes que
`infrastructure/docker/{api,web}.Dockerfile` ya construyen para Docker Compose local, nunca un segundo
artefacto de build solo para Azure.

### 2. Azure SQL con autenticación exclusiva de Microsoft Entra ID
Coherente con que Entra ID ya es "la única identidad" en todo el proyecto (pedido §5) — elimina una
contraseña SQL que rotar o filtrar. **Sin cambio de código**: `Microsoft.Data.SqlClient` soporta
`Authentication=Active Directory Managed Identity` directamente en la cadena de conexión, y
`UseSqlServer(connectionString)` (`Infrastructure/DependencyInjection.cs`, sin tocar desde F0) ya solo
pasa esa cadena tal cual — la diferencia entre local (`User Id=sa;Password=...`) y Azure es puramente de
configuración, nunca de código. La única pieza que Bicep no puede automatizar de forma confiable es crear
la identidad administrada del App Service como usuario *dentro* de la base de datos — queda como paso
manual documentado en `docs/deployment.md`, no como un recurso `deploymentScripts` que no se pudo
verificar contra una suscripción real.

### 3. Blob Storage y Storage Queue vía Key Vault, no identidad administrada
A diferencia de SQL, cambiar `AzureBlobFileStorage`/`AzureStorageQueueImportQueue` (F8/F9) a
`DefaultAzureCredential` sería un cambio de código real e imposible de probar contra Azure real en esta
sesión. En su lugar, `modules/storage.bicep` escribe la cadena de conexión de la propia Storage Account
que crea como secreto en Key Vault; los App Service la leen vía `@Microsoft.KeyVault(VaultName=...;
SecretName=...)` — cero secretos en el repositorio o en App Settings en texto plano, mismo nivel de
seguridad práctico, sin tocar el SDK. Documentado como decisión deliberada de V1, no como la opción ideal
definitiva — migrar Storage a identidad administrada queda como extensión futura real.

### 4. Nombres de recursos determinísticos vía `uniqueString(resourceGroup().id)`
Storage Account/Key Vault/Container Registry necesitan nombres únicos globalmente en Azure. En vez de
pedirle a quien despliega que los coordine a mano, cada módulo deriva su nombre de forma reproducible.
**Hallazgo real durante la construcción**: un `Microsoft.Authorization/roleAssignments` no puede tener su
`name`/`scope` calculados a partir del *output* de un módulo hermano (aunque sea determinístico) —
`bicep build` lo rechaza con `BCP120`. La fórmula de nombre de ACR/Key Vault se repite como variables
planas en `main.bicep` específicamente para los bloques de asignación de rol, en vez de depender de
`acr.outputs.name`/`keyVault.outputs.name` ahí — documentado con un comentario en el propio archivo para
que quien lo edite no intente "simplificarlo" de vuelta a una referencia de módulo y reintroduzca el error.

### 5. Sin Private Endpoints/VNet en V1
Ya documentado en `docs/deployment.md` desde F0 como *"nice-to-have no bloqueante... según costo/tiempo"*
— se mantiene esa decisión, se documenta como punto de extensión, no se construye infraestructura de red
en esta fase.

### 6. Promoción de imágenes entre registros, no reconstrucción por ambiente
Cada ambiente tiene su propio Container Registry (aislamiento completo entre `dev`/`test`/`prod`, cada
uno su propio *resource group*). La alternativa de un registro único compartido habría sido más simple,
pero mezcla el ciclo de vida de recursos de distintos ambientes. En cambio, `.github/workflows/cd.yml`
construye la imagen **una sola vez** (en `dev`) y la *promueve* sin reconstruir hacia `test` y `prod` vía
`az acr import` — el mismo artefacto (mismo *digest*) que pasó pruebas es el que llega a producción, nunca
un *rebuild* que pudiera introducir una diferencia no probada.

### 7. GitHub Actions con OIDC, ambientes como *gate* de aprobación
`azure/login@v2` con credencial federada — nunca un secreto de Azure de larga vida en GitHub. `prod` se
protege configurando un revisor requerido en el *GitHub Environment* correspondiente (Settings, no el
propio *workflow*) — `dev`/`test`/`prod` son tres *jobs* secuenciales idénticos en forma, y es la propia
plataforma de GitHub la que pausa el que lo requiera, sin lógica condicional en YAML.

## Verificado sin una suscripción real
- `bicep build` sobre `main.bicep` y los 6 módulos: sin errores ni advertencias (CLI de Bicep 0.47.16,
  standalone, descargado para esta verificación).
- `bicep build-params` sobre los tres `.bicepparam`: sin errores.
- `actionlint` (v1.7.12) sobre los tres *workflows*: sin hallazgos.
- Revisión cruzada manual: cada variable de entorno que la aplicación ya usa (confirmadas en
  `docker-compose.yml`/`.env.example` de fases anteriores — `ConnectionStrings__AssetManagementDb`,
  `BlobStorage__ConnectionString`, `Cors__AllowedOrigins__0`, `EntraId__*`, `ENTRA_*`, `AUTH_*`) está
  provista por los App Settings que `main.bicep` genera, sin inventar un nombre nuevo.

## Consecuencias
- `docs/deployment.md` es ahora la guía real y accionable (no "a definir") para el primer despliegue —
  incluye los pasos manuales que Bicep deliberadamente no automatiza (usuario SQL, secretos reales en Key
  Vault, migraciones de EF Core).
- No hay forma de verificar visualmente esta fase como F1-F12 — la verificación es de revisión y sintaxis,
  documentado con esa honestidad tanto aquí como en el resumen de cierre de la fase.
