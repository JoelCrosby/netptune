import { ProjectTypeService } from '../services/project-type/project-type.service';

export class Project {

    constructor(private projectTypeService: ProjectTypeService = null) {}

    public projectId: number;

    public name: string;
    public description: string;
    public projectTypeId: number;

}
