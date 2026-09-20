# Hardening (F12)

Hasta F11, CORS (allowlist explícito, `Cors:AllowedOrigins`) y el manejo de errores
(`GlobalExceptionHandler` — nunca expone detalle interno salvo para excepciones de negocio ya conocidas)
ya estaban endurecidos desde F0. F12 agrega lo que faltaba: *rate limiting* y encabezados de seguridad,
tanto en la API como en el frontend.

## Rate limiting (API)

`Microsoft.AspNetCore.RateLimiting` (incluido en el framework desde .NET 7 — sin paquete NuGet nuevo).
Política global de ventana fija, particionada por usuario autenticado (claim `oid` de Entra ID) o por IP
si no hay sesión:

| Clave | Por defecto | Efecto |
|---|---|---|
| `RateLimiting:PermitLimit` | `300` | Solicitudes permitidas por partición dentro de la ventana |
| `RateLimiting:WindowSeconds` | `10` | Tamaño de la ventana |

**300 solicitudes/10s es deliberadamente generoso** — el objetivo de V1 es frenar un cliente roto o
abusivo, no acotar el uso normal de la UI ni la propia suite de pruebas de integración (que ya emite
ráfagas de cientos de solicitudes por corrida; `ApiWebApplicationFactory` sube el límite a un millón
específicamente para no interferir con eso). Un cliente que excede el límite recibe `429 Too Many
Requests` con encabezado `Retry-After`. Verificado con una prueba de integración dedicada
(`RateLimitingTests.cs`) que fija su propio límite bajo para provocar el 429 de forma determinística.

**Fuera de alcance de V1** (documentado, no omitido): un WAF real de borde (Azure Front Door) y
protección DDoS administrada son infraestructura de Azure — tema de F13, no algo que esta aplicación
pueda proveerse a sí misma desde dentro del proceso.

## Encabezados de seguridad

Mismo conjunto base en API y frontend:

| Encabezado | Valor | Por qué |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | El navegador no debe adivinar el tipo de contenido de una respuesta |
| `X-Frame-Options` | `DENY` | Esta aplicación nunca debe cargarse dentro de un `<iframe>` de otro sitio (mitiga *clickjacking*) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | No filtra la URL completa (posibles datos sensibles en la ruta/query) a sitios externos |

**API** (`SecurityHeadersMiddleware`, `apps/api/src/AssetManagement.Api/Middleware/`): agrega los tres de
arriba a toda respuesta; además `app.UseHsts()` fuera de `Development` (con `Development` ya se sirve por
HTTP local sin certificado de confianza, igual que `UseHttpsRedirection` ya se comporta). Una API JSON
pura no necesita una `Content-Security-Policy` propia — no renderiza HTML.

**Frontend** (`next.config.ts`, función `headers()`): los mismos tres, más una `Content-Security-Policy`
pragmática:

```
default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';
img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';
base-uri 'self'; form-action 'self'; object-src 'none'
```

`'unsafe-inline'` en `script-src`/`style-src` es necesario para el propio *bootstrap* de hidratación de
Next.js y los estilos en línea de Tailwind/shadcn — una CSP más estricta basada en *nonces* por request es
una mejora real posible más adelante, documentada aquí como pendiente, no implementada en V1.
`next/font/google` (usado en `app/layout.tsx`) autohospeda los archivos de fuente en tiempo de build, así
que `font-src 'self'` basta — nunca se hace una petición en tiempo real a `fonts.googleapis.com`.
`connect-src 'self'` es correcto porque el navegador nunca llama directamente a la API de .NET
(`API_INTERNAL_URL` solo es alcanzable desde el servidor de Next.js) — todo pasa por *Server Actions* o
*Route Handlers* del propio origen.

## Verificado

```bash
curl -I http://localhost:3000/assets     # confirma los encabezados en una respuesta real del frontend
curl -I http://localhost:5080/api/v1/system/info   # confirma los encabezados en una respuesta real de la API
```

## Pendiente (no bloqueante, fuera de alcance de V1)

- CSP basada en *nonces* (elimina `'unsafe-inline'`).
- WAF/DDoS administrado (F13, infraestructura Azure real).
- Rotación de secretos vía Key Vault (F13).
