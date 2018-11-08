import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog, MatExpansionPanel, MatSnackBar } from '@angular/material';
import { saveAs } from 'file-saver';
import { DragulaService } from 'ng2-dragula';
import { merge, Subscription } from 'rxjs';
import { mapTo, startWith } from 'rxjs/operators';
import { fadeIn } from '../../animations';
import { ProjectTaskStatus } from '../../enums/project-task-status';
import { ProjectTask } from '../../models/project-task';
import { ProjectTaskDto } from '../../models/view-models/project-task-dto';
import { AuthService } from '../../services/auth/auth.service';
import { ProjectTaskService } from '../../services/project-task/project-task.service';
import { ProjectsService } from '../../services/projects/projects.service';
import { UtilService } from '../../services/util/util.service';
import { WorkspaceService } from '../../services/workspace/workspace.service';
import { TaskDialogComponent } from '../dialogs/task-dialog/task-dialog.component';

@Component({
    selector: 'app-project-tasks',
    templateUrl: './project-tasks.component.html',
    styleUrls: ['./project-tasks.component.scss'],
    animations: [fadeIn]
})
export class ProjectTasksComponent implements OnInit, OnDestroy {

    public exportInProgress = false;
    public dragGroupName = 'PROJECT_TASKS';

    public selectedTask: ProjectTask;
    public subs = new Subscription();

    public completedStatus = ProjectTaskStatus.Complete;
    public inProgressStatus = ProjectTaskStatus.InProgress;
    public blockedStatus = ProjectTaskStatus.OnHold;
    public backlogStatus = ProjectTaskStatus.InActive;

    public myTasks: ProjectTaskDto[] = [];
    public completedTasks: ProjectTaskDto[] = [];
    public backlogTasks: ProjectTaskDto[] = [];

    public taskspanel: MatExpansionPanel;

    public dragStart$ = this.dragulaService.drag(this.dragGroupName).pipe(mapTo(true));
    public dragEnd$ = this.dragulaService.dragend(this.dragGroupName).pipe(mapTo(false));
    public isDragging$ = merge(this.dragStart$, this.dragEnd$).pipe(startWith(false));

    public dataLoaded = false;

    constructor(
        public projectTaskService: ProjectTaskService,
        private projectsService: ProjectsService,
        private workspaceService: WorkspaceService,
        public snackBar: MatSnackBar,
        private dragulaService: DragulaService,
        private utilService: UtilService,
        private authService: AuthService,
        public dialog: MatDialog
    ) {
        this.subs.add(
            this.dragulaService
                .dropModel(this.dragGroupName)
                .subscribe(({ target, source, item }) => {

                    const task = <ProjectTask>item;
                    if (!task) { return; }

                    if (target.id !== source.id && task.status !== Number(target.id)) {
                        this.projectTaskService.changeTaskStatus(task, Number(target.id));
                    }
                })
        );
        this.subs.add(this.projectTaskService.taskUpdated
            .subscribe(_ => this.refreshData())
        );
        this.subs.add(this.projectTaskService.taskAdded
            .subscribe(_ => this.refreshData())
        );
        this.subs.add(this.projectTaskService.taskDeleted
            .subscribe(_ => this.refreshData())
        );
    }

    ngOnInit(): void {
        this.refreshData();
    }

    ngOnDestroy(): void {
        this.subs.unsubscribe();
    }

    async refreshData(): Promise<void> {
        await this.projectTaskService.refreshTasks();

        this.utilService.smoothUpdate(this.myTasks, this.projectTaskService.myTasks);
        this.utilService.smoothUpdate(this.completedTasks, this.projectTaskService.completedTasks);
        this.utilService.smoothUpdate(this.backlogTasks, this.projectTaskService.backlogTasks);

        this.dataLoaded = true;
    }

    async addProjectTask(task: ProjectTask): Promise<void> {
        await this.projectTaskService.addProjectTask(task);
    }

    showAddModal(): void {
        this.open();
    }

    open(): void {
        const dialogRef = this.dialog.open(TaskDialogComponent, {
            width: '600px'
        });

        dialogRef.afterClosed().subscribe((result: ProjectTask) => {
            if (!result) {
                return;
            }

            const newProjectTask = new ProjectTask();
            newProjectTask.name = result.name;
            newProjectTask.description = result.description;
            newProjectTask.projectId = result.projectId;
            newProjectTask.workspaceId = this.workspaceService.currentWorkspace.id;
            newProjectTask.project = this.projectsService.projects.find(x => x.id === result.id);
            newProjectTask.assigneeId = this.authService.token.userId;

            this.addProjectTask(newProjectTask);
        });
    }

    async exportProjects(): Promise<void> {
        this.exportInProgress = true;

        try {
            const result = this.projectTaskService.getTasks(this.workspaceService.currentWorkspace).toPromise();
            const blob = new Blob([JSON.stringify(result, null, '\t')], { type: 'text/plain;charset=utf-8' });
            saveAs(blob, 'projects.json');
            this.exportInProgress = false;
        } catch (error) {
            this.exportInProgress = error != null;
        }
    }
}
