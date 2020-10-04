import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Tag } from '@core/models/tag';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import * as TagSelectors from '@core/store/tags/tags.selectors';
import * as TagActions from '@core/store/tags/tags.actions';
import { Selected } from '@core/models/selected';

@Component({
  selector: 'app-board-group-tags',
  templateUrl: './board-group-tags.component.html',
  styleUrls: ['./board-group-tags.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupTagsComponent implements OnInit {
  tags$: Observable<Selected<Tag>[]>;
  loaded$: Observable<boolean>;
  selectedCount$: Observable<number>;

  constructor(private store: Store) {}

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
