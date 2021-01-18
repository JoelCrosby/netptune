import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { StaticModule } from '@static/static.module';
import { ProjectsViewComponent } from './views/projects-view/projects-view.component';
import { ProjectsRoutingModule } from './projects-routing.module';
import { ProjectListComponent } from './components/project-list/project-list.component';
import { ProjectDetailViewComponent } from './views/project-detail-view/project-detail-view.component';
import { ProjectListItemComponent } from './components/project-list-item/project-list-item.component';
import { ProjectDetailComponent } from './components/project-detail/project-detail.component';

@NgModule({
  declarations: [
    ProjectsViewComponent,
    ProjectListComponent,
    ProjectDetailViewComponent,
    ProjectListItemComponent,
    ProjectDetailComponent,
  ],
  imports: [SharedModule, StaticModule, ProjectsRoutingModule],
  providers: [],
})
export class ProjectsModule {}
