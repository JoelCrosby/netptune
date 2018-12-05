import { Component, OnInit } from '@angular/core';
import { ProjectType } from '../../models/project-type';
import { AlertService } from '../../services/alert/alert.service';
import { Router } from '../../../../node_modules/@angular/router';
import { ProjectTypeService } from '../../services/project-type/project-type.service';

@Component({
  selector: 'app-descriptors',
  templateUrl: './descriptors.component.html',
  styleUrls: ['./descriptors.component.scss']
})
export class DescriptorsComponent implements OnInit {

  public inputName: string;
  public inputDescription: string;

  closeResult: string;
  selectedProjectType: ProjectType;

  constructor(
    public projectTypeService: ProjectTypeService,
    private alertsService: AlertService,
    private router: Router) { }

  ngOnInit() {
    this.getProjectTypes();
  }

  getProjectTypes(): void {
    this.projectTypeService.refreshProjectTypes();
  }
}
