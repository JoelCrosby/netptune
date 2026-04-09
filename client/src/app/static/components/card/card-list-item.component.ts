import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { HeaderAction } from '@core/types/header-action';
import { CardComponent } from './card.component';
import { CardTitleComponent } from './card-title.component';
import { MatButton, MatAnchor } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { CardContentComponent } from './card-content.component';
import { CardActionsComponent } from './card-actions.component';

@Component({
  selector: 'app-card-list-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardComponent,
    CardTitleComponent,
    MatButton,
    MatIcon,
    CardContentComponent,
    CardActionsComponent,
    MatAnchor,
  ],
  template: `
    <app-card>
      <app-card-title>
        {{ title() }}
        <button
          mat-button
          color="primary"
          type="button"
          (click)="delete.emit()">
          <mat-icon>close</mat-icon>
        </button>
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

  readonly delete = output();
}
