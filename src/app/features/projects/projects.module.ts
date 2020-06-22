import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { StaticModule } from '@app/static/static.module';
import { ProjectsViewComponent } from './views/projects-view/projects-view.component';
import { ProjectsRoutingModule } from './projects-routing.module';
import { ProjectsListComponent } from './components/projects-list/projects-list.component';

@NgModule({
  declarations: [ProjectsViewComponent, ProjectsListComponent],
  imports: [SharedModule, StaticModule, ProjectsRoutingModule],
  providers: [],
})
export class ProjectsModule {}
