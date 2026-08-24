import { Component, input, output } from '@angular/core';
import { Field, FormField } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { Workspace } from '@core/models/workspace';
import { brandingImageUrl } from '@core/util/branding';
import {
  LucideCheck,
  LucideGalleryVerticalEnd,
  LucideLogOut,
} from '@lucide/angular';
import { PopoverSurfaceComponent } from '@static/components/popover-surface/popover-surface.component';
import { AutofocusDirective } from '@static/directives/autofocus.directive';
import { WorkspaceBadgeComponent } from './workspace-badge.component';
import { WorkspaceSelectMenuActionComponent } from './workspace-select-menu-action.component';
import { WorkspaceSelectOptionComponent } from './workspace-select-option.component';

@Component({
  selector: 'app-workspace-select-menu',
  template: `
    @if (isOpen()) {
      <app-popover-surface size="compact" enterFrom="top">
        @if (current(); as workspace) {
          <div
            class="border-border flex items-center gap-2.5 border-b px-3 py-2.5">
            <app-workspace-badge
              [color]="workspace.metaInfo?.color"
              [logoUrl]="logoUrl(workspace)"
              [letter]="workspace.name[0]" />

            <div class="min-w-0">
              <p class="truncate text-sm font-semibold">{{ workspace.name }}</p>
              <p class="text-muted truncate text-xs">/{{ workspace.slug }}</p>
            </div>
          </div>
        }

        @if (!workspaces().length) {
          <div class="flex h-9.5 items-center px-3 font-[inherit] text-sm">
            <span i18n="Shown when no workspace matches the search term">
              No results found...
            </span>
          </div>
        }

        @if (workspaces().length) {
          <input
            appAutofocus
            class="border-border text-foreground bg-card focus:border-primary m-2 appearance-none rounded-sm border px-2 py-1.5 font-[inherit] text-sm transition-colors focus:outline-none"
            i18n-placeholder="Placeholder in the workspace search box"
            placeholder="Search.."
            [formField]="searchField()"
            (click)="$event.stopPropagation()"
            autocomplete="off" />
        }

        <div
          class="custom-scroll max-h-54 scrollbar-gutter-stable overflow-y-auto px-2 pb-2">
          @for (option of filteredOptions(); track option.id) {
            <button
              app-workspace-select-option
              [active]="option.id === selected()?.id"
              (click)="optionSelect.emit(option)">
              <app-workspace-badge
                [color]="option.metaInfo?.color"
                [logoUrl]="logoUrl(option)"
                [letter]="option.name[0]" />
              <span class="flex-1 truncate">{{ option.name }}</span>

              @if (option.id === current()?.id) {
                <svg lucideCheck class="h-4 w-4 shrink-0"></svg>
              }
            </button>
          }
        </div>

        <div class="border-border flex flex-col justify-start border-t p-2">
          <a app-workspace-menu-action [routerLink]="['/workspaces']">
            <svg lucideGalleryVerticalEnd class="h-4 w-4 shrink-0"></svg>
            <span i18n="Workspace menu action that opens the workspace picker">
              Workspaces
            </span>
          </a>
          <button
            app-workspace-menu-action
            type="button"
            (click)="logout.emit()">
            <svg lucideLogOut class="h-4 w-4 shrink-0"></svg>
            <span i18n="Workspace menu action that signs the user out">
              Logout
            </span>
          </button>
        </div>
      </app-popover-surface>
    }
  `,
  imports: [
    FormField,
    AutofocusDirective,
    LucideCheck,
    LucideGalleryVerticalEnd,
    LucideLogOut,
    RouterLink,
    WorkspaceBadgeComponent,
    PopoverSurfaceComponent,
    WorkspaceSelectMenuActionComponent,
    WorkspaceSelectOptionComponent,
  ],
})
export class WorkspaceSelectMenuComponent {
  readonly isOpen = input.required<boolean>();
  readonly filteredOptions = input.required<Workspace[]>();
  readonly workspaces = input.required<Workspace[]>();
  readonly selected = input<Workspace | null>(null);
  readonly current = input<Workspace | null | undefined>(null);
  readonly searchField = input.required<Field<string>>();

  readonly optionSelect = output<Workspace>();
  readonly logout = output();

  protected logoUrl(workspace: Workspace): string | null {
    return brandingImageUrl(workspace.slug, workspace.metaInfo?.logoFileId);
  }
}
