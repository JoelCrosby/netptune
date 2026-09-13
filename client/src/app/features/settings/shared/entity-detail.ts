import { inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ClientResponse } from '@core/models/client-response';
import { ConfirmationService } from '@core/services/confirmation.service';
import { mutation } from '@core/util/mutation';
import { requireSuccess } from '@core/util/rxjs-operators';
import { Observable, finalize, firstValueFrom } from 'rxjs';

export interface EntityDeleteOptions {
  title: string;
  message: string;
  request: () => Observable<ClientResponse<unknown>>;
  onDeleted?: () => void;
  onFailed: () => void;
}

// Call in a field initialiser. A confirmed, successful delete returns to the list page.
export function entityDelete() {
  const confirmation = inject(ConfirmationService);
  const router = inject(Router);
  const route = inject(ActivatedRoute);
  const request = mutation();

  return {
    pending: request.pending,

    run: async (options: EntityDeleteOptions) => {
      const confirm = confirmation.open({
        title: options.title,
        message: options.message,
        acceptLabel: $localize`:Confirms a destructive action:Delete`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      });

      const confirmed = await firstValueFrom(confirm, { defaultValue: false });

      if (!confirmed) return;

      request.run(options.request().pipe(requireSuccess()), {
        onSuccess: () => {
          options.onDeleted?.();
          void router.navigate(['..'], { relativeTo: route });
        },
        onError: () => options.onFailed(),
      });
    },
  };
}

// Resolves with whether the server accepted the change; a thrown request still rejects.
export function entitySave() {
  const pending = signal(false);

  return {
    pending: pending.asReadonly(),

    run: async (source: Observable<ClientResponse<unknown>>) => {
      pending.set(true);

      const response = await firstValueFrom(
        source.pipe(finalize(() => pending.set(false)))
      );

      return response.isSuccess;
    },
  };
}
