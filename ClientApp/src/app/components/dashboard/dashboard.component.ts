import { Component, OnInit } from '@angular/core';
import { ProjectsService } from '../../services/projects/projects.service';
import { PostsService } from '../../services/posts/posts.service';
import { dropIn } from '../../animations';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  animations: [dropIn]
})
export class DashboardComponent implements OnInit {

  constructor(
    public projectsService: ProjectsService,
    public postService: PostsService) { }

  ngOnInit() {
  }

  showAddModal(): void {

  }

}
