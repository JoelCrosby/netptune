import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import {
  APPEARANCE_THEME,
  PreferenceDefinitionsResponse,
  PreferenceScope,
  PreferenceValueClientResponse,
  PreferenceValuesResponse,
  ResolvedPreferenceValue,
} from '@core/models/user-preferences';
import { changeTheme } from '@core/store/settings/settings.actions';
import { Store } from '@ngrx/store';
import { catchError, finalize, of, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class UserPreferencesService {
  private http = inject(HttpClient);
  private store = inject(Store);

  readonly definitions = signal<PreferenceDefinitionsResponse | null>(null);
  readonly values = signal<PreferenceValuesResponse | null>(null);
  readonly loading = signal(false);
  readonly loaded = signal(false);

  readonly groups = computed(() => this.values()?.groups ?? []);

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
      .put<PreferenceValueClientResponse>(`api/user-preferences/values/${key}`, {
        scope,
        value,
      })
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
      .delete<PreferenceValueClientResponse>(`api/user-preferences/values/${key}`, {
        params: { scope },
      })
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
            item.definition.key === preference.definition.key ? preference : item
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
    if (
      preference.definition.key === APPEARANCE_THEME &&
      typeof preference.effectiveValue === 'string'
    ) {
      this.store.dispatch(changeTheme({ theme: preference.effectiveValue }));
    }
  }
}
