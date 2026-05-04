import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import type { TurnstileObject } from 'turnstile-types';

@Injectable({ providedIn: 'root' })
export class TurnstileLoaderService {
  private document = inject(DOCUMENT);
  private loadPromise?: Promise<TurnstileObject>;

  load(): Promise<TurnstileObject> {
    if (typeof window === 'undefined') {
      return Promise.reject(
        new Error('Turnstile is only available in the browser.')
      );
    }

    if (window.turnstile) {
      return Promise.resolve(window.turnstile);
    }

    this.loadPromise ??= this.loadScript();

    return this.loadPromise;
  }

  private loadScript(): Promise<TurnstileObject> {
    return new Promise((resolve, reject) => {
      const script = this.getOrCreateScript();
      const timeoutId = window.setTimeout(handleTimeout, 10000);

      const cleanup = () => {
        window.clearTimeout(timeoutId);
        script.removeEventListener('load', handleLoad);
        script.removeEventListener('error', handleError);
      };

      const handleLoad = () => {
        cleanup();

        if (!window.turnstile) {
          reject(new Error('Turnstile loaded without exposing the API.'));
          return;
        }

        resolve(window.turnstile);
      };

      const handleError = () => {
        cleanup();
        reject(new Error('Turnstile was blocked by the browser.'));
      };

      function handleTimeout() {
        cleanup();
        reject(new Error('Turnstile failed to load.'));
      }

      script.addEventListener('load', handleLoad);
      script.addEventListener('error', handleError);

      if (script.dataset.loaded === 'true') {
        handleLoad();
      }
    });
  }

  private getOrCreateScript() {
    const existingScript = this.document.getElementById(
      'cloudflare-turnstile-api'
    );

    if (existingScript instanceof HTMLScriptElement) {
      return existingScript;
    }

    const script = this.document.createElement('script');
    script.id = 'cloudflare-turnstile-api';
    script.src =
      'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';
    script.async = true;
    script.defer = true;
    script.addEventListener('load', () => {
      script.dataset.loaded = 'true';
    });

    this.document.head.append(script);

    return script;
  }
}
