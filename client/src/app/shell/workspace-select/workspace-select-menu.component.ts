import { Component, input, output } from '@angular/core';
import { Field, FormField } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { Workspace } from '@core/models/workspace';
import { PopoverSurfaceComponent } from '@static/components/popover-surface/popover-surface.component';
import { AutofocusDirective } from '@static/directives/autofocus.directive';
import { WorkspaceBadgeComponent } from './workspace-badge.component';

@Component({
  selector: 'app-workspace-select-menu',
  template: `
    @if (isOpen()) {
      <app-popover-surface size="compact" enterFrom="top">
        @if (!workspaces().length) {
          <div class="flex h-9.5 items-center px-2 font-[inherit] text-sm">
            No results found...
          </div>
        }
        @if (workspaces().length) {
          <input
            appAutofocus
            class="border-border text-foreground bg-card sticky top-0 m-2 appearance-none border-2 p-2 font-[inherit] text-sm focus:outline-none"
            placeholder="Search.."
            [formField]="searchField()"
            (click)="$event.stopPropagation()"
            autocomplete="off" />
        }
        <div
          class="custom-scroll max-h-54 scrollbar-gutter-stable overflow-y-auto py-[.4rem] pl-[.4rem]">
          @for (option of filteredOptions(); track option.id) {
            <div
              class="hover:bg-hover my-[.2rem] flex h-9.5 cursor-pointer items-center rounded-sm px-2 font-[inherit] text-sm"
              [class.bg-primary]="option.id === selected()?.id"
              (click)="optionSelect.emit(option)">
              <app-workspace-badge
                class="mr-3.5"
                [color]="option.metaInfo?.color"
                [letter]="option.name[0]" />
              <span>{{ option.name }}</span>
            </div>
          }
        </div>
        <div
          class="border-foreground/10 bg-card flex flex-col justify-start border-t p-[.4rem]">
          <a
            class="text-foreground hover:bg-hover my-[.2rem] block cursor-pointer rounded-sm px-[.4rem] py-2 text-sm leading-6 tracking-[.225px]"
            [routerLink]="['/workspaces']">
            Workspaces
          </a>
          <div
            class="text-foreground hover:bg-hover my-[.2rem] block cursor-pointer rounded-sm px-[.4rem] py-2 text-sm leading-6 tracking-[.225px]"
            (click)="logout.emit()">
            Logout
          </div>
        </div>
      </app-popover-surface>
    }
  `,
  imports: [
    FormField,
    AutofocusDirective,
    RouterLink,
    WorkspaceBadgeComponent,
    PopoverSurfaceComponent,
  ],
})
export class WorkspaceSelectMenuComponent {
  readonly isOpen = input.required<boolean>();
  readonly filteredOptions = input.required<Workspace[]>();
  readonly workspaces = input.required<Workspace[]>();
  readonly selected = input<Workspace | null>(null);
  readonly searchField = input.required<Field<string>>();

  readonly optionSelect = output<Workspace>();
  readonly logout = output();
}
