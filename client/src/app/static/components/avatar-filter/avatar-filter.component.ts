import { Component, input, output } from '@angular/core';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';

export interface AvatarFilterOption {
  id: string;
  displayName?: string | null;
  pictureUrl?: string | null;
  isServiceAccount?: boolean;
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
                [isServiceAccount]="option.isServiceAccount ?? false"
                (click)="optionClicked.emit(option)" />
            </div>
            @if (option.online) {
              <span
                class="border-background pointer-events-none absolute right-0.5 bottom-0.5 h-3 w-3 rounded-full border-2 bg-green-500"
                [appTooltip]="presenceTooltip(option.displayName)"></span>
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
  readonly onlineLabel = input(
    $localize`:Presence state appended after a person's name, e.g. "Ada is online":is online`
  );

  readonly optionClicked = output<AvatarFilterOption>();

  /**
   * One message with both parts as placeholders rather than concatenating in the
   * template, so translators can reorder the name and the state.
   *
   * The state itself is a caller-supplied fragment, which limits how far it can
   * be reworded — acceptable while every caller passes a short presence phrase.
   */
  protected presenceTooltip(displayName: string | null | undefined): string {
    const state = this.onlineLabel();

    return $localize`:Tooltip on a presence dot. NAME is the person's display name and STATE is their presence, e.g. "is online":${displayName ?? ''}:NAME: ${state}:STATE:`;
  }
}
