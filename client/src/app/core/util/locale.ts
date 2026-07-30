/**
 * Every locale the app ships, declared once. Anything that needs a property of a
 * locale — its URL prefix, its name in the switcher — reads it from here rather
 * than keeping a parallel table keyed by locale code.
 *
 * Two things outside TypeScript have to stay in step with this list: the `i18n`
 * block in `angular.json` and the locale maps in `nginx.conf`.
 *
 * `name` is always the locale's name in its own language ("Deutsch", not
 * "German"), so a user stranded in a language they cannot read can still find
 * theirs. It is therefore deliberately NOT marked for translation.
 */
export const supportedLocales = [
  {
    code: 'en-GB',
    // Source locale, served from the root: angular.json sets its i18n subPath
    // to "", so it alone has no URL prefix.
    prefix: '',
    name: 'English (UK)',
  },
  { code: 'fr', prefix: '/fr', name: 'Français' },
  { code: 'de', prefix: '/de', name: 'Deutsch' },
  { code: 'es', prefix: '/es', name: 'Español' },
] as const;

export type Locale = (typeof supportedLocales)[number];

/** Source locale, and the fallback when the running locale is not one we ship. */
export const defaultLocale: Locale = supportedLocales[0];

export const findLocale = (code: string): Locale | undefined => {
  return supportedLocales.find((locale) => locale.code === code);
};

export const appLocale: string =
  // The guard matters under test: the karma and vitest builders auto-inject
  // `@angular/localize/init` when the package resolves, so `$localize` exists but
  // `$localize.locale` is undefined.
  (typeof $localize !== 'undefined' && $localize.locale) || defaultLocale.code;

export const dateTimeFormat = (
  options: Intl.DateTimeFormatOptions
): Intl.DateTimeFormat => {
  return new Intl.DateTimeFormat(appLocale, options);
};

export const numberFormat = (
  options: Intl.NumberFormatOptions = {}
): Intl.NumberFormat => {
  return new Intl.NumberFormat(appLocale, options);
};

/**
 * `Intl.Locale.getWeekInfo()` is only declared in `lib.esnext.intl`, and this
 * project targets ES2022, so the capability is described structurally.
 */
interface WeekInfoCapableLocale {
  getWeekInfo?: () => { firstDay: number };
}

const firstDayCache = new Map<string, number>();

/**
 * First day of the week, 0 = Sunday, matching `Date.prototype.getDay()`.
 *
 * Replaces `@angular/common`'s deprecated `getLocaleFirstDayOfWeek`. Angular now
 * points at `Intl`, but `getWeekInfo()` is not universally implemented — Firefox
 * still lacks it — so this falls back to Monday, the ISO 8601 default, which is
 * correct for en-GB, fr, de and es, every locale this app ships.
 */
export const firstDayOfWeek = (locale: string = appLocale): number => {
  const cached = firstDayCache.get(locale);

  if (cached !== undefined) {
    return cached;
  }

  let firstDay = 1;

  try {
    // getWeekInfo reports 1..7 (Mon..Sun); % 7 maps that onto getDay()'s 0..6.
    const info = (
      new Intl.Locale(locale) as WeekInfoCapableLocale
    ).getWeekInfo?.();

    if (info) {
      firstDay = info.firstDay % 7;
    }
  } catch {
    // Malformed locale tag — keep the ISO 8601 default.
  }

  firstDayCache.set(locale, firstDay);

  return firstDay;
};

export type DateNameStyle = 'narrow' | 'short' | 'long';

export const weekdayNames = (
  style: DateNameStyle,
  locale: string = appLocale
): string[] => {
  const format = new Intl.DateTimeFormat(locale, {
    weekday: style,
    timeZone: 'UTC',
  });
  const offset = firstDayOfWeek(locale);

  // 2024-01-07 was a Sunday, so adding the weekday index lands on that weekday.
  return Array.from({ length: 7 }, (_, index) => {
    return format.format(
      new Date(Date.UTC(2024, 0, 7 + ((index + offset) % 7)))
    );
  });
};

export const monthNames = (
  style: DateNameStyle,
  locale: string = appLocale
): string[] => {
  const format = new Intl.DateTimeFormat(locale, {
    month: style,
    timeZone: 'UTC',
  });

  return Array.from({ length: 12 }, (_, month) => {
    return format.format(new Date(Date.UTC(2024, month, 1)));
  });
};
