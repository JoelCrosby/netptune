import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { StaticModule } from '@app/static/static.module';
import { ProjectsComponent } from './index/projects.index.component';
import { ProjectsRoutingModule } from './projects-routing.module';

@NgModule({
  declarations: [ProjectsComponent],
  imports: [SharedModule, StaticModule, ProjectsRoutingModule],
  providers: [],
})
export class ProjectsModule {}
