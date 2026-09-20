# ADR 0003 — Importaciones masivas procesadas de forma asíncrona y desacoplada

## Estado
Aceptado (F0, 2026-09-15).

## Contexto
El pedido exige soportar hasta 10,000 filas por lote con validación completa antes de guardar, vista previa,
detección de duplicados y modo transaccional completo o solo-filas-válidas. Procesar esto de forma síncrona
dentro de un único request HTTP no es viable de forma confiable (tiempo de respuesta, reintentos de red,
tamaño de payload).

## Decisión
- El endpoint de importación sube el archivo a Blob Storage y crea un `ImportBatch` en estado `Queued`.
- Un mensaje se publica a una cola (`Azure Storage Queue` en Azure; `System.Threading.Channels` + un
  `BackgroundService` de ASP.NET Core en desarrollo local/Docker Compose, misma interfaz `IImportQueue`) para
  que un *worker* procese el lote de forma idempotente (clave de idempotencia = `ImportBatchId`).
- El usuario consulta el progreso/resultado por `ImportBatch` (estado, reporte de errores por fila y campo).
- Se eligió **Azure Storage Queue** sobre Azure Service Bus para V1 porque: (a) no agrega un recurso Azure
  adicional a los ya listados explícitamente en la sección 3 del pedido, (b) la app ya usa la misma Storage
  Account para Blob Storage, (c) el volumen (lotes de hasta 10k filas, no miles de mensajes/segundo) no
  requiere las garantías avanzadas de Service Bus (sesiones, dead-lettering avanzado) en V1. Documentado como
  supuesto no bloqueante — revisable si el volumen real de importaciones concurrentes lo justifica.

## Consecuencias
- El mismo contrato (`IImportQueue`, `ImportBatch`) funciona en local y en Azure, cambia solo la
  implementación de infraestructura.
- Permite migrar a Service Bus o Azure Functions más adelante sin tocar el dominio de importación.
