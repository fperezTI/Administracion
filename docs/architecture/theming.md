# Sistema de temas visuales

Cinco temas predefinidos (Claro, Oscuro, Corporativo Azul, Ejecutivo Gris, Alto Contraste) más un modo
"heredar de la empresa". Estrategia híbrida: los temas están definidos enteramente en código (frontend);
la base de datos solo almacena **códigos** de tema, nunca colores/CSS/JSON.

## Resumen de la resolución

```
tema efectivo = preferencia personal del usuario
             ?? tema por defecto de la empresa activa
             ?? "light" (fallback seguro, siempre válido)
```

- La preferencia personal es **global al usuario**, no por empresa ni por dispositivo — un solo valor en
  `identity.Users.ThemePreferenceCode` (nullable: `null` = "usar tema de la empresa").
- El tema por defecto de la empresa vive en `organization.Companies.DefaultThemeCode` (no nulo,
  `HasDefaultValue("light")`), modificable solo por un usuario con el permiso `Companies.Update`.
- Ambos campos son `nvarchar(30)` — nunca colores, nunca JSON. El backend valida cada código contra la
  misma lista blanca cerrada que el frontend (`AssetManagement.Domain.Theming.ThemeCode` /
  `apps/web/src/lib/theme-catalog.ts`), rechazando cualquier valor fuera de las 5 opciones con
  `DomainException` → 422.

## Por qué no hay un endpoint `GET /themes`

El frontend necesita de todos modos su propio mapeo tipado código→clase CSS→metadatos visuales (los
mismos 5 códigos, en el mismo orden, con el mismo significado) para poder aplicar el tema sin esperar una
respuesta de red. Exponerlo también desde el backend sería una fuente de verdad duplicada sin beneficio:
ambos lados ya deben coincidir por contrato, así que un round-trip solo para confirmar algo que el
frontend ya sabe es puro costo. El backend solo *valida* contra su propia copia de la lista (whitelist
cerrada), nunca la publica como catálogo.

## Por qué no se usó `next-themes`

`next-themes` resuelve el tema en el cliente (localStorage + un script de pre-hidratación insertado en
`<head>`) — apropiado para un simple claro/oscuro sin persistencia en servidor, pero incompatible con los
requisitos aquí: preferencia por usuario en base de datos, aplicable "en el login" y consistente entre
dispositivos, sin parpadeo de tema incorrecto (*flash of wrong theme*) y sin depender de
`useEffect`/localStorage como fuente primaria. Se retiró (`theme-toggle.tsx`, `theme-provider.tsx`,
dependencia `next-themes` eliminados) en favor de resolución 100% server-side (patrón BFF).

## Resolución server-side (sin parpadeo)

`apps/web/src/app/layout.tsx` (Server Component raíz) lee la sesión ya resuelta y aplica la clase
correspondiente directamente en `<html className={theme}>` — el HTML llega al navegador con el tema
correcto ya aplicado, sin script de hidratación ni reflow visible.

El "cache de presentación" para ese tema resuelto es la propia sesión cifrada de NextAuth (JWT
`httpOnly`), no una cookie nueva:

- **Al iniciar sesión** y **en cada refresco silencioso del access token** (`auth.ts`, callback `jwt`),
  `withEffectiveTheme()` llama a `GET /api/v1/me` una vez y cachea `effectiveTheme`/`themePreference` en
  el JWT. Esto es lo que permite que un cambio del tema por defecto de una empresa (hecho por un Super
  Administrador) llegue a los demás usuarios en modo "heredar" "en la siguiente carga" (pedido), sin
  cookie dedicada ni llamada extra en cada navegación.
- **Al confirmar un cambio de tema** desde el selector (`confirmThemeSelectionAction` en
  `lib/theme-actions.ts`), se llama `unstable_update()` — la API server-side de NextAuth v5 para
  refrescar el JWT de sesión activa disparando el callback `jwt` con `trigger: "update"` — de modo que el
  cambio se aplique de inmediato, sin esperar el próximo refresco de token.

Se evaluó y se descartó una cookie de tema dedicada, poblada por middleware en cada request: reintroduce
la misma latencia por request que ya se eliminó en la revisión de performance (ver
`project_assetmanagement_deployment_state` en memoria), y no aporta nada que la sesión de NextAuth no
tuviera ya.

## Selector de tema (UX)

Vive dentro del menú de perfil (`components/layout/user-menu.tsx`), no en una página nueva:

1. **`ThemeMenuItem`** — la entrada "Tema" dentro del `DropdownMenuContent`. Solo cambia estado
   (`onOpen`) en el componente padre; no contiene el diálogo.
2. **`ThemeSelectorDialog`** — el diálogo real (galería, vista previa, confirmar/restaurar, temporizador),
   montado como hermano del `DropdownMenu` en `UserMenu`, **no** anidado dentro de su contenido.

Esta separación no es solo estilo: Base UI desmonta el contenido de un `Menu` una vez que termina de
cerrarse, y `ThemeMenuItem` cierra el menú al hacer clic (comportamiento por defecto de
`Menu.Item`/`DropdownMenuItem`). Si el diálogo (con su estado de vista previa/temporizador) viviera dentro
de ese contenido, se desmontaría junto con el menú apenas se abre, perdiendo el diálogo recién abierto. El
diálogo debe ser una pieza independiente, con su estado (`open`) controlado por el padre.

**Vista previa sin persistir**: seleccionar una opción en la galería aplica la clase CSS directamente
sobre `document.documentElement` (manipulación de DOM puntual, no estado de React ni backend) e inicia un
temporizador de 15 segundos. Nada se guarda hasta "Conservar". "Restaurar", cerrar el diálogo sin
confirmar, agotar el temporizador, o desmontar el componente (navegación) revierten a la clase que estaba
aplicada antes de abrir el diálogo (capturada en un `ref` cuando `open` pasa a `true` — ver comentario en
`theme-selector.tsx` sobre por qué esto no puede vivir en el `onOpenChange` del propio diálogo).

**Vista previa de "usar tema de la empresa"**: no puede leerse de `effectiveTheme` de la sesión — ese
valor ya refleja la preferencia personal *actual* (que sigue persistida durante la vista previa), no lo
que se vería si se heredara. `previewCompanyThemeAction()` pide `GET /api/v1/me` y lee
`companies[].defaultThemeCode` de la empresa activa directamente.

## Empresa activa y su relación con "heredar tema" (decisión de arquitectura)

`docs/multi-company.md` ya documentaba que `ICurrentCompanyContext`/`X-Active-Company-Id` existían como
mecanismo pero que "aún no hay ningún módulo en F1 que lea de aquí". Este sistema de temas es el primer
consumidor real — y expuso que **ningún llamador del frontend envía ese encabezado todavía**, por lo que
`ActiveCompanyId` es `null` en la práctica para toda request actual.

En vez de construir un mecanismo nuevo de selección de "empresa activa" persistente a nivel de sesión
(fuera de alcance de este pedido, y no solicitado), `GetMeQueryHandler` usa como *fallback* la primera
empresa accesible del usuario cuando no hay una empresa activa explícita — ver ADR 0016 para el
razonamiento completo. Efecto práctico: con una sola empresa (el caso real actual), "heredar tema de la
empresa" funciona sin ambigüedad; con varias empresas y sin selector de empresa activa persistente, se
hereda de la primera por orden de alta hasta que ese selector exista.

## Accesibilidad — Alto Contraste

No es simplemente fondo negro: usa amarillo (`#f5c400`-ish, definido como token `--primary`) sobre negro,
con bordes de foco reforzados — la combinación de mayor contraste perceptible que ya usan patrones de alto
contraste de sistemas operativos (Windows High Contrast), en vez de un tema "oscuro" renombrado.

## Dónde está cada pieza

| Pieza | Archivo |
|---|---|
| Lista blanca de códigos (backend) | `apps/api/src/AssetManagement.Domain/Theming/ThemeCode.cs` |
| Preferencia del usuario | `Domain/Identity/User.cs` (`ThemePreferenceCode`, `SetThemePreference`) |
| Default de la empresa | `Domain/Organization/Company.cs` (`DefaultThemeCode`, `SetDefaultTheme`) |
| Resolución del tema efectivo | `Application/Identity/GetMeQuery.cs` |
| Endpoints | `MeController` (`PUT /me/preferences/theme`), `CompaniesController` (`PUT /companies/{id}/preferences/theme`, requiere `Companies.Update`) |
| Catálogo tipado (frontend) | `apps/web/src/lib/theme-catalog.ts` |
| Tokens CSS por tema | `apps/web/src/app/globals.css` (`.dark`, `.corporate-blue`, `.executive-gray`, `.high-contrast`) |
| Resolución server-side + cache de sesión | `apps/web/src/auth.ts` |
| Selector (UI) | `apps/web/src/components/layout/theme-selector.tsx`, `user-menu.tsx` |
| Selector de tema por empresa (admin) | `apps/web/src/app/companies/company-theme-select.tsx` |
