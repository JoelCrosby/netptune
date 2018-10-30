import { PostType } from '../enums/post-type';
import { Project } from './project';
import { Basemodel } from './basemodel';

export class Post extends Basemodel {

    public title: string;
    public body: string;
    public postType: PostType;

    public project: Project;
    public projectId: number;

}
