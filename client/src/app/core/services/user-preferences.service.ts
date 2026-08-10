import { HttpClient } from '@angular/common/http';
import { Service, computed, inject, signal } from '@angular/core';
import { ThemeService } from '@core/services/theme.service';
import {
  APPEARANCE_THEME,
  PreferenceDefinitionsResponse,
  PreferenceScope,
  PreferenceValueClientResponse,
  PreferenceValuesResponse,
  ResolvedPreferenceValue,
} from '@core/models/user-preferences';
import { catchError, finalize, of, tap } from 'rxjs';

@Service()
export class UserPreferencesService {
  private http = inject(HttpClient);
  private readonly theme = inject(ThemeService);

  readonly definitions = signal<PreferenceDefinitionsResponse | null>(null);
  readonly values = signal<PreferenceValuesResponse | null>(null);
  readonly loading = signal(false);
  readonly loaded = signal(false);

  readonly groups = computed(() => this.values()?.groups ?? []);

  /** Effective value for a preference key from the currently-loaded values. */
  effectiveValueFor(key: string): unknown {
    return this.values()
      ?.groups.flatMap((group) => group.preferences)
      .find((preference) => preference.definition.key === key)?.effectiveValue;
  }

  load() {
    if (this.loading()) return;

    this.loading.set(true);

    this.http
      .get<PreferenceValuesResponse>('api/user-preferences/values')
      .pipe(
        tap((values) => {
          this.values.set(values);
          this.loaded.set(true);
          this.applyThemePreference();
        }),
        catchError(() => of(null)),
        finalize(() => this.loading.set(false))
      )
      .subscribe();
  }

  loadDefinitions() {
    return this.http
      .get<PreferenceDefinitionsResponse>('api/user-preferences/definitions')
      .pipe(tap((definitions) => this.definitions.set(definitions)));
  }

  updateValue(key: string, scope: PreferenceScope, value: unknown) {
    return this.http
      .put<PreferenceValueClientResponse>(
        `api/user-preferences/values/${key}`,
        {
          scope,
          value,
        }
      )
      .pipe(
        tap((response) => {
          if (response.payload) {
            this.replacePreference(response.payload);
            this.applyPreferenceSideEffects(response.payload);
          }
        })
      );
  }

  deleteValue(key: string, scope: PreferenceScope) {
    return this.http
      .delete<PreferenceValueClientResponse>(
        `api/user-preferences/values/${key}`,
        {
          params: { scope },
        }
      )
      .pipe(
        tap((response) => {
          if (response.payload) {
            this.replacePreference(response.payload);
            this.applyPreferenceSideEffects(response.payload);
          }
        })
      );
  }

  private replacePreference(preference: ResolvedPreferenceValue) {
    this.values.update((current) => {
      if (!current) return current;

      return {
        groups: current.groups.map((group) => ({
          ...group,
          preferences: group.preferences.map((item) =>
            item.definition.key === preference.definition.key
              ? preference
              : item
          ),
        })),
      };
    });
  }

  private applyThemePreference() {
    const preference = this.values()
      ?.groups.flatMap((group) => group.preferences)
      .find((preference) => preference.definition.key === APPEARANCE_THEME);

    if (preference) {
      this.applyPreferenceSideEffects(preference);
    }
  }

  private applyPreferenceSideEffects(preference: ResolvedPreferenceValue) {
    const isTheme = preference.definition.key === APPEARANCE_THEME;

    if (!isTheme) {
      return;
    }

    /**
     * A theme nobody chose falls back to the browser's preference rather than
     * the definition's default, which would put every new account on light.
     */
    const hasChosenTheme = preference.source !== 'default';

    if (!hasChosenTheme) {
      this.theme.clear();

      return;
    }

    if (typeof preference.effectiveValue === 'string') {
      this.theme.set(preference.effectiveValue);
    }
  }
}
