import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { HeaderAction } from '@core/types/header-action';
import { LucideX } from '@lucide/angular';
import { FlatButtonComponent } from '../button/flat-button.component';
import { CardActionsComponent } from './card-actions.component';
import { CardContentComponent } from './card-content.component';
import { CardTitleComponent } from './card-title.component';
import { CardComponent } from './card.component';

@Component({
  selector: 'app-card-list-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardComponent,
    CardTitleComponent,
    FlatButtonComponent,
    LucideX,
    CardContentComponent,
    CardActionsComponent,
  ],
  template: `
    <app-card>
      <app-card-title>
        {{ title() }}

        @if (canDelete()) {
          <button
            app-flat-button
            color="ghost"
            type="button"
            (click)="delete.emit()">
            <svg lucideX [size]="28"></svg>
          </button>
        }
      </app-card-title>
      <app-card-content>
        <small>{{ description() }}</small>
        <small class="text-muted"> {{ subText() }} </small>

        <ng-content />
      </app-card-content>

      <app-card-actions [actions]="actions()" />
    </app-card>
  `,
})
export class CardListItemComponent {
  readonly title = input.required<string>();
  readonly description = input.required<string>();
  readonly subText = input<string>();
  readonly subTextLabel = input<string>();
  readonly actions = input<HeaderAction[]>([]);
  readonly canDelete = input<boolean>(false);

  readonly delete = output();
}
