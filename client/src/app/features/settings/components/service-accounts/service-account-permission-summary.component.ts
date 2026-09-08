import { Component, computed, input, signal } from '@angular/core';
import { Permission } from '@core/auth/permissions';
import { LucideCheck, LucideShieldCheck, LucideX } from '@lucide/angular';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';
import { PermissionAreaChipComponent } from './permission-area-chip.component';
import {
  PermissionAreaLevel,
  PermissionAreaSummary,
  PermissionGrant,
  permissionGroups,
} from './service-account-permissions';

const totalPermissionCount = permissionGroups.reduce((total, group) => {
  return total + group.permissions.length;
}, 0);

@Component({
  selector: 'app-service-account-permission-summary',
  imports: [
    InlineButtonComponent,
    LucideCheck,
    LucideShieldCheck,
    LucideX,
    PermissionAreaChipComponent,
  ],
  template: `
    <div class="flex flex-col gap-3">
      <div class="flex flex-wrap items-center gap-x-2 gap-y-1">
        <svg
          lucideShieldCheck
          class="h-4 w-4"
          [class]="headlineIconClass()"></svg>
        @if (isFullAccess()) {
          <span
            class="text-sm font-medium"
            i18n="
              Headline on a service account that holds every workspace
              permission
            ">
            Full workspace access
          </span>
        } @else {
          <span
            class="text-sm font-medium"
            i18n="
              Headline on a service account that holds only some workspace
              permissions
            ">
            Scoped access
          </span>
        }
        @if (grantedCount() > 0) {
          <span
            class="text-muted text-sm"
            i18n="
              Summarises the size of a service account's grant. GRANTED is how
              many permissions it holds, TOTAL how many exist, and the plural
              covers how many permission areas they fall into
            ">
            {{
              grantedCount() // i18n(ph="GRANTED")
            }}
            of
            {{
              totalCount // i18n(ph="TOTAL")
            }}
            permissions across
            {activeAreaCount(), plural,
              =1 {1 area}
              other {{{ activeAreaCount() }} areas}
            }
          </span>
        }
      </div>

      @if (grantedCount() === 0) {
        <p
          class="text-muted text-sm"
          i18n="Shown when a service account has no permissions">
          No permissions granted
        </p>
      } @else {
        <div class="grid gap-1.5 sm:grid-cols-2 xl:grid-cols-3">
          @for (area of visibleAreas(); track area.key) {
            <app-permission-area-chip
              [area]="area"
              [expanded]="expanded() === area.key"
              (toggled)="toggleArea(area)" />
          }
        </div>

        @if (expandedArea(); as area) {
          <div
            class="border-primary/30 bg-primary/6 flex flex-col gap-2 rounded-md border px-3.5 py-3">
            <p
              class="text-foreground/76 text-[11px] font-semibold tracking-[0.08em] uppercase">
              <span>{{ area.label }}</span>
              <span
                i18n="
                  How much of one permission area a service account holds. Keep
                  the leading separator. GRANTED and TOTAL are counts
                ">
                —
                {{
                  area.granted // i18n(ph="GRANTED")
                }}
                of
                {{
                  area.total // i18n(ph="TOTAL")
                }}
                granted
              </span>
            </p>
            <div class="flex flex-wrap gap-1.5">
              @for (permission of area.permissions; track permission.key) {
                <span
                  class="inline-flex items-center gap-1.5 rounded px-2 py-0.5 text-xs"
                  [class]="permissionClass(permission)">
                  @if (permission.granted) {
                    <svg lucideCheck class="h-3 w-3 shrink-0"></svg>
                  } @else {
                    <svg lucideX class="h-3 w-3 shrink-0"></svg>
                  }
                  {{ permission.label }}
                </span>
              }
            </div>
          </div>
        }

        <div class="flex flex-wrap items-center gap-x-3 gap-y-1">
          @if (!showEmptyAreas()) {
            <span
              class="text-foreground/58 text-[12.5px]"
              i18n="
                Says how many permission areas a service account cannot touch
              ">
              {emptyAreaCount(), plural,
                =0 {Every area in the workspace.}
                =1 {No access to 1 other area.}
                other {No access to {{ emptyAreaCount() }} other areas.}
              }
            </span>
          }
          @if (emptyAreaCount() > 0) {
            <button
              app-inline-button
              class="text-[13px]"
              (click)="toggleEmptyAreas()">
              @if (showEmptyAreas()) {
                <span
                  i18n="
                    Button that hides the permission areas a service account
                    cannot touch
                  ">
                  Show granted only
                </span>
              } @else {
                <span
                  i18n="
                    Button that reveals the permission areas a service account
                    cannot touch
                  ">
                  View all areas
                </span>
              }
            </button>
          }
        </div>
      }
    </div>
  `,
})
export class ServiceAccountPermissionSummaryComponent {
  readonly permissions = input.required<Permission[]>();

  private readonly expandedKey = signal<string | null>(null);
  private readonly revealEmptyAreas = signal(false);

  readonly expanded = this.expandedKey.asReadonly();
  readonly showEmptyAreas = this.revealEmptyAreas.asReadonly();

  readonly totalCount = totalPermissionCount;

  readonly areas = computed<PermissionAreaSummary[]>(() => {
    const granted = new Set(this.permissions());

    return permissionGroups.map((group) => {
      const permissions = group.permissions.map((permission) => ({
        key: permission.key,
        label: permission.label,
        granted: granted.has(permission.key),
      }));

      const grantedCount = permissions.filter(
        (permission) => permission.granted
      ).length;

      return {
        key: group.key,
        label: group.label,
        granted: grantedCount,
        total: permissions.length,
        level: areaLevel(grantedCount, permissions.length),
        permissions,
      };
    });
  });

  readonly grantedCount = computed(() => {
    return this.areas().reduce((total, area) => total + area.granted, 0);
  });

  readonly activeAreaCount = computed(() => {
    return this.areas().filter((area) => area.granted > 0).length;
  });

  readonly emptyAreaCount = computed(() => {
    return this.areas().length - this.activeAreaCount();
  });

  readonly isFullAccess = computed(() => {
    return this.grantedCount() === this.totalCount;
  });

  readonly visibleAreas = computed(() => {
    if (this.revealEmptyAreas()) return this.areas();

    return this.areas().filter((area) => area.granted > 0);
  });

  readonly expandedArea = computed(() => {
    const key = this.expandedKey();

    if (key === null) return null;

    return this.visibleAreas().find((area) => area.key === key) ?? null;
  });

  readonly headlineIconClass = computed(() => {
    if (this.grantedCount() === 0) return 'text-muted';

    return this.isFullAccess() ? 'text-change-added' : 'text-change-modified';
  });

  toggleArea(area: PermissionAreaSummary) {
    this.expandedKey.update((key) => (key === area.key ? null : area.key));
  }

  toggleEmptyAreas() {
    this.revealEmptyAreas.update((reveal) => !reveal);
  }

  protected permissionClass(permission: PermissionGrant): string {
    return permission.granted
      ? 'bg-foreground/9 text-foreground'
      : 'text-foreground/52';
  }
}

function areaLevel(granted: number, total: number): PermissionAreaLevel {
  if (granted === 0) return 'none';

  return granted === total ? 'full' : 'partial';
}
