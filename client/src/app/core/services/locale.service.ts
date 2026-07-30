import { DOCUMENT, Location } from '@angular/common';
import { inject, Injectable, LOCALE_ID } from '@angular/core';
import {
  defaultLocale,
  findLocale,
  supportedLocales,
  type Locale,
} from '@core/util/locale';

const localeCookieName = 'nt_locale';
const localeCookieMaxAge = 60 * 60 * 24 * 365;

@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly document = inject(DOCUMENT);
  private readonly location = inject(Location);
  private readonly localeId = inject(LOCALE_ID);

  readonly locales = supportedLocales;
  readonly current: Locale = findLocale(this.localeId) ?? defaultLocale;

  switchTo(locale: Locale): void {
    if (locale.code === this.current.code) {
      return;
    }

    const view = this.document.defaultView;

    if (view == null) {
      return;
    }

    const secure = view.location.protocol === 'https:' ? '; secure' : '';

    this.document.cookie =
      `${localeCookieName}=${locale.code}; path=/; max-age=${localeCookieMaxAge}; samesite=lax` +
      secure;

    const path = this.location.path(true) || '/';

    view.location.assign(`${locale.prefix}${path}`);
  }
}
