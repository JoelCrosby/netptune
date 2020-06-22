import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { StaticModule } from '@app/static/static.module';
import { ProjectsViewComponent } from './views/projects-view/projects-view.component';
import { ProjectsRoutingModule } from './projects-routing.module';
import { ProjectsListComponent } from './components/projects-list/projects-list.component';
import { ProjectDetailViewComponent } from './views/project-detail-view/project-detail-view.component';

@NgModule({
  declarations: [ProjectsViewComponent, ProjectsListComponent, ProjectDetailViewComponent],
  imports: [SharedModule, StaticModule, ProjectsRoutingModule],
  providers: [],
})
export class ProjectsModule {}
