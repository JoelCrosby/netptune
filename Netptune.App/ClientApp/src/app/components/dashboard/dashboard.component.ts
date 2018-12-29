import { Component, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { Post } from '../../models/post';
import { dropIn } from '../../animations';
import { PostsService } from '../../services/posts/posts.service';
import { ProjectsService } from '../../services/projects/projects.service';
import { BoardPostDialogComponent } from '../../dialogs/board-post-dialog/board-post-dialog.component';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  animations: [dropIn]
})
export class DashboardComponent implements OnInit {

  constructor(
    public projectsService: ProjectsService,
    public postsService: PostsService,
    private dialog: MatDialog,
    private snackbar: MatSnackBar
  ) { }

  ngOnInit() {
  }

  async commentButtonClicked(): Promise<void> {
    const dialogRef = this.dialog.open(BoardPostDialogComponent, {
      width: '600px'
    });

    const result: Post = await dialogRef.afterClosed().toPromise();
    if (!result) {
      return;
    }

    await this.postsService.savePost(result);
  }

}
