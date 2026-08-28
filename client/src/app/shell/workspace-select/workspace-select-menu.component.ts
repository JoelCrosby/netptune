import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { Field, FormField } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { Workspace } from '@core/models/workspace';
import { brandingImageUrl } from '@core/util/branding';
import {
  LucideCheck,
  LucideGalleryVerticalEnd,
  LucideLogOut,
  LucidePlus,
  LucideSearch,
} from '@lucide/angular';
import { PopoverSurfaceComponent } from '@static/components/popover-surface/popover-surface.component';
import { KeyboardKeyComponent } from '@static/components/keyboard-key/keyboard-key.component';
import { AutofocusDirective } from '@static/directives/autofocus.directive';
import { WorkspaceBadgeComponent } from './workspace-badge.component';
import { WorkspaceSelectMenuActionComponent } from './workspace-select-menu-action.component';
import { WorkspaceSelectOptionComponent } from './workspace-select-option.component';

@Component({
  selector: 'app-workspace-select-menu',
  template: `
    <ng-template #row let-option>
      <button
        app-workspace-select-option
        [active]="option.id === selected()?.id"
        [current]="option.id === current()?.id"
        (click)="optionSelect.emit(option)">
        <app-workspace-badge
          size="sm"
          [color]="option.metaInfo?.color"
          [logoUrl]="logoUrl(option)"
          [letter]="option.name[0]" />
        <span class="flex-1 truncate">{{ option.name }}</span>

        @if (option.id === current()?.id) {
          <svg lucideCheck class="text-primary h-3.75 w-3.75 shrink-0"></svg>
        } @else if (option.id === selected()?.id) {
          <span
            class="font-avatar text-[10px] text-[rgba(var(--foreground-rgb),0.3)]">
            &#8629;
          </span>
        }
      </button>
    </ng-template>

    <ng-template #empty>
      <div class="flex h-9.5 items-center px-3 font-[inherit] text-sm">
        <span i18n="Shown when no workspace matches the search term">
          No results found...
        </span>
      </div>
    </ng-template>

    @if (isOpen()) {
      <app-popover-surface size="sheet" enterFrom="top" [leaving]="leaving()">
        <div
          class="border-border flex h-10.5 shrink-0 items-center gap-2.25 border-b px-3">
          <svg
            lucideSearch
            class="h-3.75 w-3.75 shrink-0 text-[rgba(var(--foreground-rgb),0.45)]"
            aria-hidden="true"></svg>
          <input
            appAutofocus
            class="text-foreground min-w-0 flex-1 appearance-none border-none bg-transparent p-0 font-[inherit] text-sm placeholder:text-[rgba(var(--foreground-rgb),0.4)] focus:ring-0 focus:outline-none"
            i18n-placeholder="Placeholder in the workspace search box"
            placeholder="Search workspaces"
            [formField]="searchField()"
            (click)="$event.stopPropagation()"
            autocomplete="off" />
          <app-keyboard-key [class]="keyHintClass">{{
            escapeKeyLabel
          }}</app-keyboard-key>
        </div>

        <div
          class="custom-scroll max-h-54 scrollbar-gutter-stable overflow-y-auto">
          @if (searching()) {
            <div class="px-2 py-2">
              @for (option of filteredOptions(); track option.id) {
                <ng-container
                  [ngTemplateOutlet]="row"
                  [ngTemplateOutletContext]="{ $implicit: option }" />
              } @empty {
                <ng-container [ngTemplateOutlet]="empty" />
              }
            </div>
          } @else {
            @if (recentOptions().length) {
              <div class="px-2 pt-2 pb-1">
                <div
                  class="font-avatar px-2 pt-1 pb-1.5 text-[10px] tracking-[.12em] text-[rgba(var(--foreground-rgb),0.35)]"
                  i18n="
                    Heading above the recently visited workspaces in the
                    workspace switcher
                  ">
                  RECENT
                </div>
                @for (option of recentOptions(); track option.id) {
                  <ng-container
                    [ngTemplateOutlet]="row"
                    [ngTemplateOutletContext]="{ $implicit: option }" />
                }
              </div>
            }

            @if (otherOptions().length) {
              <div class="px-2 pt-1 pb-2">
                <div
                  class="font-avatar px-2 pt-1 pb-1.5 text-[10px] tracking-[.12em] text-[rgba(var(--foreground-rgb),0.35)]"
                  i18n="
                    Heading above the remaining workspaces in the workspace
                    switcher
                  ">
                  ALL WORKSPACES
                </div>
                @for (option of otherOptions(); track option.id) {
                  <ng-container
                    [ngTemplateOutlet]="row"
                    [ngTemplateOutletContext]="{ $implicit: option }" />
                }
              </div>
            }

            @if (!recentOptions().length && !otherOptions().length) {
              <div class="py-2">
                <ng-container [ngTemplateOutlet]="empty" />
              </div>
            }
          }
        </div>

        <div class="border-border shrink-0 border-t p-1.5">
          <button
            app-workspace-menu-action
            type="button"
            class="text-[rgba(var(--foreground-rgb),0.8)]"
            (click)="createWorkspace.emit()">
            <svg lucidePlus class="text-primary h-3.75 w-3.75 shrink-0"></svg>
            <span
              class="flex-1"
              i18n="Workspace menu action that creates a new workspace">
              New workspace
            </span>
            <app-keyboard-key [class]="keyHintClass">{{
              createKeyLabel
            }}</app-keyboard-key>
          </button>

          <a
            app-workspace-menu-action
            class="text-[rgba(var(--foreground-rgb),0.8)]"
            [routerLink]="['/workspaces']"
            (click)="manage.emit()">
            <svg
              lucideGalleryVerticalEnd
              class="h-3.75 w-3.75 shrink-0 opacity-60"></svg>
            <span
              class="flex-1"
              i18n="Workspace menu action that opens the workspace picker">
              Manage workspaces
            </span>
            <app-keyboard-key [class]="keyHintClass">{{
              manageKeyLabel
            }}</app-keyboard-key>
          </a>

          <button
            app-workspace-menu-action
            type="button"
            class="text-[rgba(var(--foreground-rgb),0.55)]"
            (click)="logout.emit()">
            <svg lucideLogOut class="h-3.75 w-3.75 shrink-0 opacity-60"></svg>
            <span
              class="flex-1"
              i18n="Workspace menu action that signs the user out">
              Log out
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
    LucidePlus,
    LucideSearch,
    NgTemplateOutlet,
    RouterLink,
    WorkspaceBadgeComponent,
    KeyboardKeyComponent,
    PopoverSurfaceComponent,
    WorkspaceSelectMenuActionComponent,
    WorkspaceSelectOptionComponent,
  ],
})
export class WorkspaceSelectMenuComponent {
  readonly isOpen = input.required<boolean>();
  readonly leaving = input(false);
  readonly filteredOptions = input.required<Workspace[]>();
  readonly recentOptions = input.required<Workspace[]>();
  readonly otherOptions = input.required<Workspace[]>();
  readonly selected = input<Workspace | null>(null);
  readonly current = input<Workspace | null | undefined>(null);
  readonly searchField = input.required<Field<string>>();
  readonly searchTerm = input('');

  readonly optionSelect = output<Workspace>();
  readonly createWorkspace = output();
  readonly manage = output();
  readonly logout = output();

  /** Quieter and squarer than the default key cap, to suit a menu row. */
  protected readonly keyHintClass =
    'min-w-0 shrink-0 rounded-sm border-[rgba(var(--foreground-rgb),0.14)] px-[5px] text-[10px] font-normal text-[rgba(var(--foreground-rgb),0.4)]';

  protected readonly escapeKeyLabel = $localize`:Keyboard key that closes the workspace menu, shown as a hint:esc`;

  // Letter keys read the same in every locale.
  protected readonly createKeyLabel = 'N';
  protected readonly manageKeyLabel = '\u21e7W';

  /** A query collapses the two groups into one flat list of matches. */
  protected readonly searching = computed(() => !!this.searchTerm());

  protected logoUrl(workspace: Workspace): string | null {
    return brandingImageUrl(workspace.slug, workspace.metaInfo?.logoFileId);
  }
}
