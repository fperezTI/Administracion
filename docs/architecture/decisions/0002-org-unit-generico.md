# ADR 0002 — Estructura organizacional como `OrgUnit` genérico jerárquico

## Estado
Aceptado (F0, 2026-09-15).

## Contexto
El pedido (sección 9) exige administrar 11 tipos de elementos organizacionales (unidad de negocio, dirección,
gerencia, departamento, área, equipo, sucursal, ubicación física, almacén, centro de datos, proyecto), todos
jerárquicos, con historial y reorganización sin perder trazabilidad.

## Decisión
Modelar un único agregado `OrgUnit` con:
- `OrgUnitTypeId` (catálogo configurable `OrgUnitType`, sembrado con los 11 tipos iniciales, extensible).
- `ParentOrgUnitId` (jerarquía auto-referenciada).
- `CompanyId` (raíz del árbol).
- Historial de reorganización vía `OrgUnitHistory` (registro de cambios de padre/tipo/estado, nunca se
  sobreescribe el pasado).

En vez de 11 tablas casi idénticas. `Warehouse` (almacén) es un `OrgUnitType` pero además implementa una
interfaz de dominio adicional (`IStockLocation`) para las reglas específicas de existencias de consumibles.

## Consecuencias
- Un único módulo de administración de estructura organizacional (alta, edición, desactivación,
  reorganización) sirve para los 11 tipos, reduciendo superficie de código sin perder semántica de negocio
  (el tipo sigue siendo explícito y configurable, no se pierde distinción).
- Riesgo: consultas que necesiten reglas muy distintas por tipo (p. ej. capacidad de almacén) requieren
  extensiones específicas (`WarehouseDetails` como tabla complementaria 1:1) en vez de columnas genéricas —
  se aplica cuando el tipo lo requiera, no de forma anticipada.
