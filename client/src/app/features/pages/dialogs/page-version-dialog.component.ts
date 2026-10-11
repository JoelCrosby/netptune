import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { PanelComponent } from '@static/components/panel.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { TextDiffComponent } from '@static/components/text-diff/text-diff.component';
import { DiffMode } from '@static/components/text-diff/text-diff';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { pageVersionResource } from '../resources/pages.resource';

export interface PageVersionDialogData {
  pageId: number;
  versionId: number;
  heading: string;
  canRestore: boolean;
}

export type PageVersionDialogResult = 'restore';

@Component({
  selector: 'app-page-version-dialog',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    SegmentedControlComponent,
    TextDiffComponent,
    PanelComponent,
    PageLoadingComponent,
    ErrorStateComponent,
  ],
  host: { class: 'block' },
  template: `
    <div class="mb-3 flex items-center gap-3">
      <app-dialog-title noMargin class="min-w-0 flex-1">{{
        data.heading
      }}</app-dialog-title>
      <app-segmented-control
        variant="outlined"
        i18n-ariaLabel="Accessible label for the switch between diff layouts"
        ariaLabel="Diff layout"
        [options]="modeOptions"
        [(value)]="mode" />
    </div>

    <app-panel class="max-h-[60vh] overflow-y-auto">
      @if (version.value(); as version) {
        <app-text-diff
          [before]="version.previousBody"
          [after]="version.body"
          [mode]="mode()"
          [beforeLabel]="previousLabel"
          [afterLabel]="thisLabel" />
      } @else if (version.error()) {
        <app-error-state
          [retryable]="false"
          i18n-title="Shown when a saved version of a page cannot be loaded"
          title="This version couldn't be loaded" />
      } @else {
        <app-page-loading />
      }
    </app-panel>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Closes the dialog showing a page version">Close</span>
      </button>
      @if (data.canRestore) {
        <button
          app-flat-button
          type="button"
          [disabled]="!version.value()"
          (click)="onRestore()">
          <span i18n="Button that restores the page to the version shown"
            >Restore this version</span
          >
        </button>
      }
    </div>
  `,
})
export class PageVersionDialogComponent {
  static readonly width = '880px';

  protected readonly data = inject<PageVersionDialogData>(DIALOG_DATA);
  private readonly dialogRef =
    inject<DialogRef<PageVersionDialogResult>>(DialogRef);

  private readonly key = signal({
    pageId: this.data.pageId,
    versionId: this.data.versionId,
  });

  protected readonly version = pageVersionResource(this.key);
  protected readonly mode = signal<DiffMode>('unified');
  protected readonly previousLabel = $localize`:Column heading above the version before the one being viewed:Previous version`;
  protected readonly thisLabel = $localize`:Column heading above the page version being viewed:This version`;

  protected readonly modeOptions: SegmentedOption<DiffMode>[] = [
    {
      value: 'unified',
      label: $localize`:Names the single column diff layout:Unified`,
    },
    {
      value: 'split',
      label: $localize`:Names the side by side diff layout:Side by side`,
    },
    {
      value: 'inline',
      label: $localize`:Names the diff layout that highlights changed words:Word level`,
    },
  ];

  protected onRestore() {
    this.dialogRef.close('restore');
  }
}
