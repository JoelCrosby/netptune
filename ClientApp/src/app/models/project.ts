import { ProjectType } from './project-type';
import { ProjectTypeService } from '../services/project-type/project-type.service';
import { IDisposable } from '../interfaces/IDisposable';

export class Project implements IDisposable {

    public isDirty: boolean;

    markDirty(): void {
      this.isDirty = true;
    }

    constructor(private projectTypeService: ProjectTypeService = null) {}

    public projectId: number;

    public name: string;
    public description: string;
    public projectTypeId: number;

}
