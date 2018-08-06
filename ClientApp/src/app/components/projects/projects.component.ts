import { Component, OnInit } from '@angular/core';
import { NgbModal, ModalDismissReasons } from '@ng-bootstrap/ng-bootstrap';
import { Project } from '../../models/project';
import { ProjectsService } from '../../projects.service';
import { FormControl, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-projects',
  templateUrl: './projects.component.html',
  styleUrls: ['./projects.component.scss']
})
export class ProjectsComponent implements OnInit {

  public projects: Project[];
  public showAddForm = false;

  public inputName: string;
  public inputDescription: string;
  public inputType: string;

  closeResult: string;
  formGroup: FormGroup;

  constructor(private projectsService: ProjectsService, private modalService: NgbModal) {

  }

  ngOnInit(): void {
    this.getProjects();

    this.formGroup = new FormGroup({
      Name: new FormControl('', [
        Validators.required,
        Validators.minLength(4),
        Validators.maxLength(128)
      ]),
      Description: new FormControl('', [
        Validators.required,
        Validators.minLength(4),
        Validators.maxLength(128)
      ])
    });
  }

  trackById(index: number, project: Project) {
    return project.projectId;
  }

  getProjects(): void {
    this.projectsService.getProjects()
    .subscribe(projects => this.projects = projects);
  }

  deleteProject(index: number): void {
    this.projectsService.deleteProject(this.projects[index])
      .subscribe();
  }

  open(content) {
    this.modalService.open(content, { centered: true }).result.then((result) => {
      this.closeResult = `Closed with: ${result}`;

      const newProject = new Project();
      newProject.name = this.inputName;
      newProject.description = this.inputDescription;
      newProject.type = this.inputType;

      this.projectsService.addProject(newProject)
        .subscribe((projectResult) => {
          if (projectResult) { this.getProjects(); }
      });
    }, (reason) => {
      this.closeResult = `Dismissed ${this.getDismissReason(reason)}`;
    });
  }

  private getDismissReason(reason: any): string {
    if (reason === ModalDismissReasons.ESC) {
      return 'by pressing ESC';
    } else if (reason === ModalDismissReasons.BACKDROP_CLICK) {
      return 'by clicking on a backdrop';
    } else {
      return `with: ${reason}`;
    }
  }

}
