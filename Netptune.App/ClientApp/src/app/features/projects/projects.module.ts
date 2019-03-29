import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { StoreModule } from '@ngrx/store';
import { ProjectsComponent } from './index/projects.index.component';
import { projectsReducer } from './store/projects.reducer';
import { EffectsModule } from '@ngrx/effects';
import { ProjectsEffects } from './store/projects.effects';
import { ProjectsRoutingModule } from './projects-routing.module';

@NgModule({
  declarations: [ProjectsComponent],
  imports: [
    SharedModule,
    StoreModule.forFeature('projects', projectsReducer),
    EffectsModule.forFeature([ProjectsEffects]),
    ProjectsRoutingModule,
  ],
})
export class ProjectsModule {}
