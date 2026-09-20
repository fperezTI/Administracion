# ADR 0004 — El código de etiqueta (`AssetTag.Code`) es único globalmente, no por empresa

## Estado
Aceptado (F2, 2026-09-16). Corrige un defecto real encontrado durante la verificación de F2 (ver más abajo).

## Contexto
El pedido (§12) exige que la etiqueta física incluya "Folio visible" e "Identificador legible por máquina"
como dos piezas de contenido. La primera implementación de `CreateAssetCommand` reutilizó el `InternalFolio`
(único solo por empresa, p. ej. `ASSET-000001` se repite en cada empresa) también como `AssetTag.Code`, y ese
campo tenía un índice único a nivel de toda la base de datos.

Esto provocó un error real durante las pruebas de integración: dos empresas distintas, cada una dando de
alta su primer activo, generaron el mismo folio (`ASSET-000001`) y la segunda inserción falló por violar el
índice único de `AssetTags.Code` (`Cannot insert duplicate key row ... IX_AssetTags_Code`).

## Decisión
`AssetTag.Code` se genera de forma **independiente** del folio (un GUID compacto, `Guid.NewGuid("N")`), y es
el valor que codifica el QR/código de barras. El folio interno sigue siendo el texto legible impreso en la
etiqueta, obtenido por separado desde `Asset.InternalFolio`. `AssetTag.Code` mantiene su índice único global
— correcto ahora, porque un mismo escaneo de QR/NFC/RFID debe resolver sin ambigüedad a un único activo sin
importar desde qué empresa se escanee (la app no necesariamente conoce la empresa antes de escanear).

## Consecuencias
- El folio (`InternalFolio`) sigue siendo único solo por empresa, como exige la sección 8 del pedido
  (prefijos/secuencias por empresa).
- El identificador de máquina (`AssetTag.Code`) es único en todo el sistema, evitando colisiones entre
  empresas al escanear.
- La etiqueta impresa muestra ambos valores por separado — ninguno de los dos deja de cumplir su requisito
  original.
