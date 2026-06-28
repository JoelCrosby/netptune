import { Component, input, output } from '@angular/core';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';

export interface AvatarFilterOption {
  id: string;
  displayName?: string | null;
  pictureUrl?: string | null;
  selected?: boolean;
  online?: boolean;
}

@Component({
  selector: 'app-avatar-filter',
  imports: [AvatarComponent, TooltipDirective],
  template: `
    @if (options().length) {
      <div class="inline-flex flex-row-reverse items-center">
        @for (option of options(); track option.id) {
          <div
            class="relative inline-flex not-last:-ml-3 hover:z-100"
            [style.z-index]="option.selected ? 99 : null">
            <div
              class="bg-background inline-flex h-10 w-10 cursor-pointer items-center justify-center overflow-hidden rounded-full border-4"
              [class.border-transparent]="!option.selected"
              [class.border-primary]="option.selected">
              <app-avatar
                size="lg"
                [name]="option.displayName"
                [imageUrl]="option.pictureUrl"
                (click)="optionClicked.emit(option)" />
            </div>
            @if (option.online) {
              <span
                class="border-background pointer-events-none absolute right-0.5 bottom-0.5 h-3 w-3 rounded-full border-2 bg-green-500"
                [appTooltip]="option.displayName + ' ' + onlineLabel()">
              </span>
            }
          </div>
        }
      </div>
    } @else if (emptyLabel()) {
      <div class="flex h-10 items-center">
        <div
          class="text-foreground/50 px-2 text-sm font-medium whitespace-nowrap select-none">
          {{ emptyLabel() }}
        </div>
      </div>
    }
  `,
})
export class AvatarFilterComponent {
  readonly options = input<AvatarFilterOption[]>([]);
  readonly emptyLabel = input<string | null>(null);
  readonly onlineLabel = input('is online');

  readonly optionClicked = output<AvatarFilterOption>();
}
