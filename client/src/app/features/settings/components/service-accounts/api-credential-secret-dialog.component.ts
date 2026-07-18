import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { ApiCredentialCreated } from '@core/models/service-account';
import { LucideCheck, LucideCopy, LucideTriangleAlert } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

@Component({
  selector: 'app-api-credential-secret-dialog',
  imports: [
    LucideTriangleAlert,
    LucideCopy,
    LucideCheck,
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `<app-dialog-title>Save API Credential</app-dialog-title>

    <div app-dialog-content>
      <div
        class="border-warn/40 bg-warn/5 mb-5 flex gap-3 rounded border px-4 py-3">
        <svg lucideTriangleAlert class="text-warn mt-0.5 h-5 w-5 shrink-0"></svg>
        <p class="text-sm">
          This secret is shown once. Copy it now and store it in a secret
          manager. Netptune cannot recover it later.
        </p>
      </div>

      <label class="mb-2 block text-sm font-medium" for="api-credential-secret">
        {{ credential.name }}
      </label>
      <textarea
        id="api-credential-secret"
        class="border-border bg-background min-h-28 w-full resize-none rounded border p-3 font-mono text-xs"
        readonly
        [value]="credential.token"
        (focus)="$any($event.target).select()"></textarea>

      @if (copyError()) {
        <p class="text-warn mt-2 text-sm">
          The browser could not copy the secret automatically. Select it above
          and copy it manually.
        </p>
      }
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="copy()">
        @if (copied()) {
          <svg lucideCheck class="h-4 w-4"></svg>
          Copied
        } @else {
          <svg lucideCopy class="h-4 w-4"></svg>
          Copy Secret
        }
      </button>
      <button app-flat-button type="button" (click)="close()">
        I Have Saved It
      </button>
    </div>`,
})
export class ApiCredentialSecretDialogComponent {
  private readonly dialogRef =
    inject<DialogRef<void, ApiCredentialSecretDialogComponent>>(DialogRef);

  readonly credential = inject<ApiCredentialCreated>(DIALOG_DATA);
  readonly copied = signal(false);
  readonly copyError = signal(false);

  async copy() {
    try {
      await navigator.clipboard.writeText(this.credential.token);
      this.copied.set(true);
      this.copyError.set(false);
    } catch {
      this.copyError.set(true);
    }
  }

  close() {
    this.dialogRef.close();
  }
}
