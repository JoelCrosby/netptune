import { Dialog, DialogConfig, DialogRef } from '@angular/cdk/dialog';
import { ComponentType } from '@angular/cdk/portal';
import { Injectable } from '@angular/core';
import { DialogContainerComponent } from '@static/components/dialog/dialog-container.component';

@Injectable({ providedIn: 'root' })
export class DialogService {
  constructor(private dialog: Dialog) {}

  open<R = unknown, D = unknown, C = unknown>(
    component: ComponentType<C>,
    config?: DialogConfig<D, DialogRef<R, C>>
  ): DialogRef<R, C> {
    config ??= new DialogConfig();

    return this.dialog.open<R, D, C>(component, {
      ...config,
      container: {
        type: DialogContainerComponent,
        providers: () => [{ provide: DialogConfig, useValue: config }],
      },
    });
  }
}
