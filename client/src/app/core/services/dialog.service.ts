import { Dialog, DialogConfig, DialogRef } from '@angular/cdk/dialog';
import { ComponentType } from '@angular/cdk/portal';
import { Injectable } from '@angular/core';
import { DialogComponent } from '@static/components/dialog/dialog.component';

@Injectable({ providedIn: 'root' })
export class DialogService {
  constructor(private dialog: Dialog) {}

  open<R = unknown, D = unknown, C = unknown>(
    component: ComponentType<C>,
    config?: DialogConfig<D, DialogRef<R, C>>
  ): DialogRef<R, C> {
    return this.dialog.open(component, {
      ...config,
      container: DialogComponent,
    });
  }
}
