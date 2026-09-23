/** Logo de 4 cuadros de Microsoft — identifica el proveedor de identidad en el botón de inicio de
 * sesión (guía de marca pública de "Sign in with Microsoft"), no es un logo propio de la app. */
export function MicrosoftLogo({ className, ...props }: React.ComponentProps<"svg">) {
  return (
    <svg viewBox="0 0 21 21" className={className} aria-hidden {...props}>
      <rect x="1" y="1" width="9" height="9" fill="#f25022" />
      <rect x="11" y="1" width="9" height="9" fill="#7fba00" />
      <rect x="1" y="11" width="9" height="9" fill="#00a4ef" />
      <rect x="11" y="11" width="9" height="9" fill="#ffb900" />
    </svg>
  );
}
