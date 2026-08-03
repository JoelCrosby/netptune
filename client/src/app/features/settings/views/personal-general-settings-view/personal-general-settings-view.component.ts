import { Component, computed, inject } from '@angular/core';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { LucideSettings2 } from '@lucide/angular';
import { PreferenceListComponent } from '@settings/components/preference-list/preference-list.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';

const NOTIFICATION_GROUP = 'notifications';

@Component({
  selector: 'app-personal-general-settings-view',
  imports: [
    IconTileComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PreferenceListComponent,
    SkeletonComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for general personal settings"
        title="General" />

      @if (isInitialLoad()) {
        <div
          class="border-border bg-card rounded-lg border p-6 shadow-sm"
          role="status"
          i18n-aria-label="Accessible label while personal settings load"
          aria-label="Loading settings">
          <app-skeleton class="h-4 w-40" />

          <div class="mt-6 flex flex-col gap-5">
            @for (row of skeletonRows; track $index) {
              <div class="flex flex-wrap items-end gap-3">
                <app-skeleton class="h-10 flex-1" />
                <app-skeleton class="h-10 w-46" />
                <app-skeleton class="h-10 w-20 shrink-0" />
              </div>
            }
          </div>
        </div>
      } @else {
        <div class="flex flex-col gap-6">
          @for (group of groups(); track group.key) {
            <section
              class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
              <header class="border-border border-b px-6 py-5">
                <div class="flex min-w-0 items-center gap-3">
                  <app-icon-tile [icon]="groupIcon" />
                  <h2 class="font-overpass truncate text-base font-semibold">
                    {{ group.label }}
                  </h2>
                </div>
              </header>

              <app-preference-list [values]="group.preferences" />
            </section>
          }
        </div>
      }
    </app-page-container>
  `,
})
export class PersonalGeneralSettingsViewComponent {
  private readonly preferences = inject(UserPreferencesService);

  protected readonly groupIcon = LucideSettings2;
  protected readonly skeletonRows = Array.from({ length: 3 });

  protected readonly groups = computed(() => {
    return this.preferences
      .groups()
      .filter((group) => group.key !== NOTIFICATION_GROUP)
      .map((group) => {
        return {
          ...group,
          preferences: group.preferences.filter(
            (preference) => !preference.definition.internal
          ),
        };
      })
      .filter((group) => group.preferences.length > 0);
  });

  protected readonly isInitialLoad = computed(() => {
    return !this.preferences.loaded() && this.groups().length === 0;
  });

  constructor() {
    this.preferences.load();
  }
}
