import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Tag } from '@core/models/tag';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import * as TagSelectors from '@core/store/tags/tags.selectors';
import * as TagActions from '@core/store/tags/tags.actions';

@Component({
  selector: 'app-board-group-tags',
  templateUrl: './board-group-tags.component.html',
  styleUrls: ['./board-group-tags.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupTagsComponent implements OnInit {
  tags$: Observable<Tag[]>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.tags$ = this.store.select(TagSelectors.selectTasks);
  }

  trackByTag(_: number, tag: Tag) {
    return tag.id;
  }

  onClicked() {
    this.store.dispatch(TagActions.loadTags());
  }
}
