import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { Tag } from '@core/models/tag';
import * as actions from '@core/store/tags/tags.actions';
import { selectTags } from '@core/store/tags/tags.selectors';
import { Actions, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { Subject } from 'rxjs';
import { takeUntil, tap } from 'rxjs/operators';

@Component({
  selector: 'app-tags',
  templateUrl: './tags.component.html',
  styleUrls: ['./tags.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TagsComponent implements OnInit, OnDestroy {
  tag$ = this.store.select(selectTags);
  onDestroy$ = new Subject();

  addTagActive: boolean;
  editTagIndex: number | null = null;

  constructor(
    private store: Store,
    private actions$: Actions,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.store.dispatch(actions.loadTags());
    this.listenForEditActions();
    this.listenForAddActions();
  }

  listenForEditActions() {
    this.actions$
      .pipe(
        takeUntil(this.onDestroy$),
        ofType(actions.editTag, actions.editTagSuccess, actions.editTagFail),
        tap((action) => {
          if (action.type !== actions.editTagSuccess.type) {
          } else {
            this.editTagIndex = null;
          }
          this.cd.detectChanges();
        })
      )
      .subscribe();
  }

  listenForAddActions() {
    this.actions$
      .pipe(
        takeUntil(this.onDestroy$),
        ofType(actions.addTag, actions.addTagSuccess, actions.addTagFail),
        tap((action) => {
          if (action.type !== actions.addTagSuccess.type) {
          } else {
            this.addTagActive = false;
          }
          this.cd.detectChanges();
        })
      )
      .subscribe();
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  onItemClicked(index: number) {
    this.editTagIndex = index;
    this.addTagActive = false;
  }

  onEditTagSubmit(value: string, tag: Tag) {
    if (!value) return;

    const currentValue = tag.name;
    const newValue = value;

    this.store.dispatch(actions.editTag({ currentValue, newValue }));
  }

  onEditCanceled() {
    this.editTagIndex = null;
    this.cd.detectChanges();
  }

  onAddTagClicked() {
    this.editTagIndex = null;
    this.addTagActive = true;
  }

  onAddTagSubmit(name: string) {
    this.store.dispatch(actions.addTag({ name }));
  }

  onAddCanceled() {
    this.addTagActive = false;
    this.cd.detectChanges();
  }

  onDeleteClicked(tag: Tag) {
    if (!tag) return;

    const tags = [tag.name];
    this.store.dispatch(actions.deleteTags({ tags }));
  }
}
