import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
} from '@angular/core';
import { AppState } from '@core/core.state';
import { EntityType } from '@core/models/entity-type';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';
import * as ActivityActions from '@core/store/activity/activity.actions';
import * as ActivitySelectors from '@core/store/activity/activity.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-activity-menu',
  templateUrl: './activity-menu.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivityMenuComponent implements OnInit {
  @Input() entityType!: EntityType;
  @Input() entityId?: number;

  activities$!: Observable<ActivityViewModel[]>;
  loaded$!: Observable<boolean>;

  constructor(private store: Store<AppState>) {}

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
