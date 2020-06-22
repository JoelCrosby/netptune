import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { SettingsViewComponent } from './views/settings-view/settings-view.component';

const routes: Routes = [{ path: '**', component: SettingsViewComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class SettingsRoutingModule {}
