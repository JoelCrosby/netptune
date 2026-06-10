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
import { CardHeaderComponent } from './card-header.component';
import { CardTitleComponent } from './card-title.component';
import { CardComponent } from './card.component';
import { CardDeleteComponent } from './card-delete.component';

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
    CardHeaderComponent,
    CardDeleteComponent,
  ],
  template: `
    <app-card>
      <app-card-header>
        <app-card-title>
          {{ title() }}
        </app-card-title>

        <app-card-delete>
          @if (canDelete()) {
            <button
              app-flat-button
              color="ghost"
              type="button"
              (click)="delete.emit()">
              <svg lucideX [size]="28"></svg>
            </button>
          }
        </app-card-delete>
      </app-card-header>
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
