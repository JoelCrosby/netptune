import { PostType } from '../enums/post-type';
import { Project } from './project';
import { Basemodel } from './basemodel';

export interface Post extends Basemodel {
  title: string;
  body: string;
  postType: PostType;

  project: Project;
  projectId: number;
}
