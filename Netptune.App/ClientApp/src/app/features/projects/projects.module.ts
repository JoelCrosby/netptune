import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { ProjectsComponent } from './index/projects.index.component';
import { ProjectsRoutingModule } from './projects-routing.module';
import { StoreModule } from '@ngrx/store';
import { EffectsModule } from '@ngrx/effects';
import { projectsReducer } from './store/projects.reducer';
import { ProjectsEffects } from './store/projects.effects';
import { ProjectsService } from './store/projects.service';
import { StaticModule } from '@app/static/static.module';

@NgModule({
  declarations: [ProjectsComponent],
  imports: [
    SharedModule,
    StaticModule,
    StoreModule.forFeature('projects', projectsReducer),
    EffectsModule.forFeature([ProjectsEffects]),
    ProjectsRoutingModule,
  ],
  providers: [ProjectsService],
})
export class ProjectsModule {}
