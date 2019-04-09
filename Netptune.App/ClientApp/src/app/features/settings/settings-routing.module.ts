import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { SettingsIndexComponent } from './index/settings.index.component';

const routes: Routes = [{ path: '**', component: SettingsIndexComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class SettingsRoutingModule {}
