import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Field, FormField } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { Workspace } from '@core/models/workspace';
import { AutofocusDirective } from '@static/directives/autofocus.directive';
import { WorkspaceBadgeComponent } from './workspace-badge.component';

@Component({
  selector: 'app-workspace-select-menu',
  template: `
    @if (isOpen()) {
      <div
        class="flex flex-col origin-top h-full text-left overflow-x-hidden border border-border bg-card menu-scale-in"
      >
        @if (!workspaces().length) {
          <div class="h-[38px] flex items-center px-2 text-sm font-[inherit]">
            No results found...
          </div>
        }
        @if (workspaces().length) {
          <input
            appAutofocus
            class="text-sm font-[inherit] p-2 m-2 appearance-none sticky top-0 border-2 border-border text-foreground bg-card focus:outline-none"
            placeholder="Search.."
            [formField]="searchField()"
            (click)="$event.stopPropagation()"
            autocomplete="off"
          />
        }
        <div class="py-[.4rem] pl-[.4rem] max-h-[216px] overflow-y-auto custom-scroll [scrollbar-gutter:stable]">
          @for (option of filteredOptions(); track option.id) {
            <div
              class="h-[38px] flex items-center px-2 cursor-pointer text-sm font-[inherit] rounded-sm my-[.2rem] hover:bg-hover"
              [class.bg-primary]="option.id === selected()?.id"
              (click)="optionSelect.emit(option)"
            >
              <app-workspace-badge
                class="mr-[14px]"
                [color]="option.metaInfo?.color"
                [letter]="option.name[0]"
              />
              <span>{{ option.name }}</span>
            </div>
          }
        </div>
        <div
          class="p-[.4rem] flex flex-col justify-start border-t border-foreground/10 bg-card"
        >
          <a
            class="cursor-pointer h-6 leading-6 block py-[.2rem] px-[.4rem] my-[.2rem] text-sm tracking-[.225px] rounded-sm text-foreground hover:bg-hover"
            [routerLink]="['/workspaces']"
          >
            Workspaces
          </a>
          <div
            class="cursor-pointer h-6 leading-6 block py-[.2rem] px-[.4rem] my-[.2rem] text-sm tracking-[.225px] rounded-sm text-foreground hover:bg-hover"
            (click)="logout.emit()"
          >
            Logout
          </div>
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, AutofocusDirective, RouterLink, WorkspaceBadgeComponent],
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
