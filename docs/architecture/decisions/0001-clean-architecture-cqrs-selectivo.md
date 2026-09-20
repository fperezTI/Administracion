# ADR 0001 — Clean Architecture con CQRS selectivo

## Estado
Aceptado (F0, 2026-09-15).

## Contexto
El pedido exige Clean Architecture, DDD ligero y "CQRS únicamente donde aporte valor", sin sobreingeniería.
Sin un criterio explícito, cada desarrollador tendería a aplicar CQRS de forma inconsistente.

## Decisión
- 4 proyectos físicos: `Domain`, `Application`, `Infrastructure`, `API`.
- MediatR con comandos/consultas separados **solo** en los módulos con reglas de negocio, validación o
  transaccionalidad no triviales: Assets, Inventory Operations, Approvals, Maintenance, Requests,
  Import/Export.
- Catálogos y configuración simple (marca, moneda, unidad de medida, proveedor) usan *application services*
  CRUD directos, sin comando/consulta por operación.
- Toda regla de negocio crítica vive en `Domain` o `Application`, nunca en controladores, componentes de UI
  ni procedimientos almacenados.

## Consecuencias
- Consistencia predecible: un desarrollador nuevo puede inferir el patrón a partir del módulo.
- Menor ceremonia en catálogos, sin sacrificar rigor donde el dominio lo exige.
