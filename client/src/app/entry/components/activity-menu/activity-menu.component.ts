import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
} from '@angular/core';
import { EntityType } from '@core/models/entity-type';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';
import * as ActivityActions from '@core/store/activity/activity.actions';
import * as ActivitySelectors from '@core/store/activity/activity.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { MatButton } from '@angular/material/button';
import { MatTooltip } from '@angular/material/tooltip';
import {
  MatMenuTrigger,
  MatMenu,
  MatMenuContent,
} from '@angular/material/menu';
import { MatIcon } from '@angular/material/icon';
import { NgIf, NgFor, AsyncPipe } from '@angular/common';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
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
    NgIf,
    NgFor,
    AvatarComponent,
    MatProgressSpinner,
    AsyncPipe,
    ActivityPipe,
  ],
})
export class ActivityMenuComponent implements OnInit {
  @Input() entityType!: EntityType;
  @Input() entityId?: number;

  activities$!: Observable<ActivityViewModel[]>;
  loaded$!: Observable<boolean>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.activities$ = this.store.select(ActivitySelectors.selectActivities);
    this.loaded$ = this.store.select(ActivitySelectors.selectActivitiesLoaded);
  }

  trackByActivity(index: number) {
    return index;
  }

  onClicked() {
    const entityType = this.entityType;
    const entityId = this.entityId;

    if (entityId === undefined) return;

    this.store.dispatch(ActivityActions.loadActivity({ entityType, entityId }));
  }

  onClosed() {
    this.store.dispatch(ActivityActions.clearState());
  }
}
