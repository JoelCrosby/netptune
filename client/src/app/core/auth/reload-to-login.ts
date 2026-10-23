// Resolving against the base href keeps the locale build (/fr/, /de/, /es/).
export function reloadToLogin() {
  window.location.assign(new URL('auth/login', document.baseURI));
}
