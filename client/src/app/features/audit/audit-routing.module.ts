import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuditViewComponent } from './views/audit-view/audit-view.component';

const routes: Routes = [{ path: '**', component: AuditViewComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AuditRoutingModule {}
