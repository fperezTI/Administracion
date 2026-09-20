# ADR 0007 — El folio se regenera al completar una transferencia entre empresas; `AssetTag.Code` no cambia

## Estado
Aceptado (F5, 2026-09-17).

## Contexto
`docs/multi-company.md` describe el ciclo de vida de una transferencia entre empresas como "aprobación +
salida + tránsito + recepción con firma", y dice textualmente que la transferencia preserva "folio e
historial general del activo". Al construir F5 apareció una tensión real con una decisión ya tomada en
ADR 0004: `Asset.InternalFolio` es único **solo por empresa** (índice `(CompanyId, InternalFolio)`),
precisamente para poder distinguirlo de `AssetTag.Code` (único globalmente). Si el folio literal se
preservara tal cual al cambiar de empresa, dos empresas independientes podrían terminar con el mismo texto
de folio en secuencias que nunca se coordinaron entre sí — por ejemplo, si la empresa destino ya folioó su
propio primer activo como `ASSET-000001` y el activo transferido también se llamaba `ASSET-000001` en su
empresa de origen, insertarlo tal cual violaría el índice único de la empresa destino.

## Decisión
- **`InternalFolio` se regenera** en la secuencia de la empresa destino al completarse la recepción,
  usando el mismo `IFolioGenerator` que se usa al dar de alta un activo — un folio es, por diseño,
  propiedad de una empresa, no del activo en sí.
- **`AssetTag.Code` nunca cambia.** Es la identidad que de verdad sobrevive a la transferencia — de hecho,
  esta fase es la prueba de que ADR 0004 se diseñó correctamente para este caso exacto: un identificador
  legible por máquina que debe resolver sin ambigüedad a un único activo sin importar desde qué empresa se
  escanee.
- **"Preserva... historial general"** se cumple de otra forma: ningún `Movement`/`Assignment`/`Loan`
  anterior se borra ni se reescribe — quedan visibles con su folio y empresa de origen tal como
  ocurrieron. Una transferencia genera **dos** registros `Movement` (uno en la empresa origen,
  `CrossCompanyTransferOut`, y otro en la empresa destino, `CrossCompanyTransferIn`), no uno, porque el
  filtro de consulta global es por una sola `CompanyId` por fila — así cada empresa ve su propia mitad de
  la historia sin necesitar debilitar ese filtro.
- `Asset.CompleteCrossCompanyTransfer` (el único método que puede reasignar `CompanyId`, cumpliendo la
  regla 3 de `CLAUDE.md` por primera vez) también limpia `CurrentOrgUnitId` — un `OrgUnit` pertenece a una
  empresa específica, así que la referencia anterior queda inválida. Ubicar el activo en la estructura de
  la empresa destino se deja al `RelocateAssetCommand` ya existente desde F3, no se duplica esa lógica
  aquí.

## Consecuencias
- `multi-company.md` se actualiza para reflejar esta resolución explícitamente.
- Verificado por integración: el flujo E2E completo confirma que `CompanyId`/`InternalFolio` cambian y
  `AssetTag.Code` no, y que ambas empresas ven su propio `Movement` correspondiente.
- Reimprimir la etiqueta física con el folio nuevo queda como acción manual del administrador en la
  empresa destino (botón "Reimprimir etiqueta" ya existente desde F2) — no se dispara automáticamente,
  para no sorprender con una reimpresión no solicitada.
