import { Component, OnInit, Input } from '@angular/core';
import { Post } from '../../models/post';
import { PostsService } from '../../services/posts/posts.service';

@Component({
  selector: 'app-board-posts',
  templateUrl: './board-posts.component.html',
  styleUrls: ['./board-posts.component.scss']
})
export class BoardPostsComponent implements OnInit {

  @Input() posts: Post[] = [];

  constructor(public postsService: PostsService) { }

  ngOnInit() {
  }

  trackById(index: number, post: Post) {
    return post.id;
  }

}
