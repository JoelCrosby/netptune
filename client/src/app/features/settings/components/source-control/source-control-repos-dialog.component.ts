import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject } from '@angular/core';
import {
  SourceControlConnection,
  sourceControlProviderNames,
} from '@core/models/source-control';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { SourceControlRepoPickerComponent } from '@shared/components/source-control-repo-picker/source-control-repo-picker.component';

export interface SourceControlReposDialogData {
  connection: SourceControlConnection;
}

@Component({
  selector: 'app-source-control-repos-dialog',
  imports: [
    DialogActionsDirective,
    DialogTitleComponent,
    FlatButtonComponent,
    SourceControlRepoPickerComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title showCloseButton [subtitle]="subtitle">
      <span i18n="Title of the repository picker dialog"
        >Choose Repositories</span
      >
    </app-dialog-title>

    <app-source-control-repo-picker #picker [connection]="data.connection" />

    <div app-dialog-actions class="justify-between">
      <button
        app-flat-button
        color="ghost"
        type="button"
        [disabled]="picker.refresh.pending()"
        (click)="picker.sync()">
        <span i18n="Button that reloads the repository list from the provider"
          >Refresh List</span
        >
      </button>
      <button app-stroked-button type="button" (click)="close()">
        <span i18n="Closes the repository picker">Done</span>
      </button>
    </div>
  `,
})
export class SourceControlReposDialogComponent {
  protected readonly data = inject<SourceControlReposDialogData>(DIALOG_DATA);
  private readonly dialogRef =
    inject<DialogRef<unknown, SourceControlReposDialogComponent>>(DialogRef);

  protected readonly subtitle = `${sourceControlProviderNames[this.data.connection.provider]} · ${this.data.connection.accountLogin}`;

  protected close() {
    this.dialogRef.close();
  }
}
