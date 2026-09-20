# Estrategia de pruebas

## Pirámide

| Nivel | Herramienta | Alcance |
|---|---|---|
| Unitarias | xUnit + FluentAssertions | Reglas de dominio: máquina de estados de `Asset`, motor de aprobaciones, cálculo de folios, validadores de Application (FluentValidation) |
| Integración | xUnit + Testcontainers (SQL Server, Azurite) | EF Core (migraciones, query filters multiempresa), autenticación/autorización (JWT de prueba), Blob Storage |
| Contrato | OpenAPI + pruebas de esquema | Estabilidad de la API versionada |
| End-to-end | Playwright | Flujos críticos (lista abajo) |
| Accesibilidad | `@axe-core/playwright` integrado en los E2E | WCAG 2.1 AA en pantallas principales |
| Seguridad | CodeQL, `dotnet list package --vulnerable`, Dependabot, pruebas de autorización negativas | OWASP Top 10, intentos de autoaprobación, acceso sin permiso, IDOR entre empresas |
| Carga | `LoadTests.cs` (xUnit + `ApiWebApplicationFactory`, "equivalente" a k6) | Escenarios de lectura/escritura representativos de la sección 35 a escala moderada — ver `docs/performance.md` para metodología, resultados y por qué la escala de referencia completa (250k/2M/500 concurrentes) queda para F13 |

## Flujos end-to-end mínimos (obligatorios, sección 35 del pedido)

1. Alta y etiquetado de un activo.
2. Asignación con firma.
3. Rechazo de recepción.
4. Devolución.
5. Transferencia entre empresas.
6. Mantenimiento (apertura → cierre con condición final).
7. Solicitud y aprobación.
8. Importación masiva.
9. Consumo de inventario (consumible).
10. Corrección mediante movimiento compensatorio.
11. Validación de permisos (acceso denegado sin permiso).
12. Intento de autoaprobación (debe rechazarse).
13. Auditoría (verificar que la acción quedó registrada correctamente).

Cada corrección de defecto futura debe incluir una prueba de regresión cuando sea viable (requisito
explícito del pedido).

## Convenciones

- Backend: proyectos `tests/Backend.UnitTests`, `tests/Backend.IntegrationTests` (ver estructura del
  monorepo). Un proyecto de pruebas por capa relevante, nombrado `{Capa}.Tests` cuando crezca lo suficiente.
- Frontend: `tests/e2e` con Playwright a nivel de monorepo (cubre la aplicación desplegada localmente vía
  Docker Compose).
- Todo módulo nuevo se entrega con sus pruebas unitarias antes de considerarse terminado (Definición de
  Terminado, sección 39 del pedido).
