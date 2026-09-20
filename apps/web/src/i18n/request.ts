import { getRequestConfig } from "next-intl/server";

// V1 ships Spanish only, but the app is wired through next-intl from day one so adding a
// second locale later means adding a messages file and a locale switcher, not restructuring
// the app. See docs/architecture/overview.md (Frontend) and the pedido's internationalization
// requirements (section 27).
export const defaultLocale = "es" as const;
export const locales = ["es"] as const;

export default getRequestConfig(async () => {
  const locale = defaultLocale;

  return {
    locale,
    messages: (await import(`../../messages/${locale}.json`)).default,
  };
});
