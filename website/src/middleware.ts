import { createMiddleware } from '@solidjs/start/middleware';
import { getRequestEvent } from 'solid-js/web';

// Solid's hydration bootstrap and streamed data are inline scripts whose content can change
// between releases, so they carry a per-request nonce rather than a pinned hash.
const contentSecurityPolicy = (nonce: string) => {
  return [
    "default-src 'self'",
    `script-src 'self' 'nonce-${nonce}' https://static.cloudflareinsights.com`,
    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
    "connect-src 'self' https://cloudflareinsights.com",
    "img-src 'self' data: https:",
    "font-src 'self' https://fonts.gstatic.com",
    "frame-ancestors 'none'",
  ].join('; ');
};

export default createMiddleware([
  (event) => {
    const nonce = crypto.randomUUID();

    getRequestEvent()!.locals.nonce = nonce;
    event.res.headers.set('Content-Security-Policy', contentSecurityPolicy(nonce));
  },
]);
