import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
} from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import {
  MatMenu,
  MatMenuContent,
  MatMenuTrigger,
} from '@angular/material/menu';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltip } from '@angular/material/tooltip';
import { EntityType } from '@core/models/entity-type';
import * as ActivityActions from '@core/store/activity/activity.actions';
import {
  selectActivities,
  selectActivitiesLoaded,
} from '@core/store/activity/activity.selectors';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { ActivityPipe } from '@static/pipes/activity.pipe';

@Component({
  selector: 'app-activity-menu',
  templateUrl: './activity-menu.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButton,
    MatTooltip,
    MatMenuTrigger,
    MatIcon,
    MatMenu,
    MatMenuContent,
    AvatarComponent,
    MatProgressSpinner,
    ActivityPipe,
  ],
})
export class ActivityMenuComponent {
  private store = inject(Store);

  readonly entityType = input.required<EntityType>();
  readonly entityId = input<number>();

  readonly activities = this.store.selectSignal(selectActivities);
  readonly loaded = this.store.selectSignal(selectActivitiesLoaded);

  trackByActivity(index: number) {
    return index;
  }

  onClicked() {
    const entityType = this.entityType();
    const entityId = this.entityId();

    if (entityId === undefined) return;

    this.store.dispatch(ActivityActions.loadActivity({ entityType, entityId }));
  }

  onClosed() {
    this.store.dispatch(ActivityActions.clearState());
  }
}
