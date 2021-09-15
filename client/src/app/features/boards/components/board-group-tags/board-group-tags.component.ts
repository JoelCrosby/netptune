import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AppState } from '@core/core.state';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import * as TagActions from '@core/store/tags/tags.actions';
import * as TagSelectors from '@core/store/tags/tags.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-board-group-tags',
  templateUrl: './board-group-tags.component.html',
  styleUrls: ['./board-group-tags.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupTagsComponent implements OnInit {
  tags$!: Observable<Selected<Tag>[]>;
  loaded$!: Observable<boolean>;
  selectedCount$!: Observable<number>;

  constructor(private store: Store<AppState>) {}

  ngOnInit() {
    this.tags$ = this.store.select(TagSelectors.selectTasksWithSelect);
    this.loaded$ = this.store.select(TagSelectors.selectTagsLoaded);
    this.selectedCount$ = this.store.select(
      TagSelectors.selectSelectedTagCount
    );
  }

  trackByTag(_: number, tag: Selected<Tag>) {
    return tag.id;
  }

  onClicked() {
    this.store.dispatch(TagActions.loadTags());
  }

  onOptionClicked(selected: Selected<Tag>) {
    const tag = selected.name;
    this.store.dispatch(TagActions.toggleSelectedTag({ tag }));
  }
}
