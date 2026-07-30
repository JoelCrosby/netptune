/**
 * Used by the `docker-dev` build configuration, which produces a container image
 * that behaves like production but is built unoptimised and points at a local
 * Aspire stack.
 *
 * apiEndpoint is relative, exactly as in production, so requests go through the
 * nginx `/api/` proxy rather than straight to the API — that is the point of the
 * image, since it exercises nginx.conf as well as the app.
 *
 * The Turnstile key is the always-passes test key, because the production sitekey
 * only validates on the real domain.
 */
export const environment = {
  apiEndpoint: '/',
  production: false,
  turnstileSitekey: '1x00000000000000000000AA',
};
