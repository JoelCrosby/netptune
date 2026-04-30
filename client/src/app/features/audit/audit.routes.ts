import { Routes } from '@angular/router';
import { AuditViewComponent } from './views/audit-view/audit-view.component';

export const routes: Routes = [{ path: '**', component: AuditViewComponent }];
