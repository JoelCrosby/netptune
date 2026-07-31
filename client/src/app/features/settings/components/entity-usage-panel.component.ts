import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  EntityUsage,
  UsageReferenceGroup,
  UsageReferenceKind,
} from '@core/models/entity-usage';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-entity-usage-panel',
  imports: [RouterLink],
  host: { class: 'block' },
  template: `
    @if (groups().length) {
      <div class="divide-border flex flex-col divide-y">
        @for (group of groups(); track group.kind) {
          <div class="px-4 py-3">
            <h4
              class="text-muted mb-2 text-xs font-medium tracking-wide uppercase">
              @switch (group.kind) {
                @case (referenceKind.project) {
                  <span i18n="Heading above projects that use a status">
                    Projects using it as their default status
                  </span>
                }
                @case (referenceKind.boardGroup) {
                  <span i18n="Heading above board columns that use a status">
                    Board columns
                  </span>
                }
                @default {
                  <span
                    i18n="
                      Heading above automation rules that reference an item
                    ">
                    Automation rules
                  </span>
                }
              }
            </h4>

            <ul class="flex flex-col gap-1">
              @for (item of group.items; track item.id) {
                <li class="flex items-baseline gap-2 text-sm">
                  @if (routeFor(group.kind); as route) {
                    <a
                      class="truncate hover:underline"
                      [routerLink]="['/', workspaceId(), route, item.id]">
                      {{ item.name }}
                    </a>
                  } @else {
                    <span class="truncate">{{ item.name }}</span>
                  }

                  @if (item.context) {
                    <span class="text-muted truncate text-xs">
                      {{ item.context }}
                    </span>
                  }
                </li>
              }
            </ul>
          </div>
        }
      </div>
    }
  `,
})
export class EntityUsagePanelComponent {
  private readonly store = inject(Store);

  readonly usage = input<EntityUsage | undefined>();

  readonly referenceKind = UsageReferenceKind;
  readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  readonly groups = computed<UsageReferenceGroup[]>(() => {
    return this.usage()?.references ?? [];
  });

  routeFor(kind: UsageReferenceKind) {
    if (kind === UsageReferenceKind.project) return 'projects';
    if (kind === UsageReferenceKind.automationRule) return 'automations';

    return null;
  }
}
