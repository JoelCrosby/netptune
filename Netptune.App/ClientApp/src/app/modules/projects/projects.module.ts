import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProjectsRoutingModule } from './projects-routing.module';
import { ProjectsComponent } from './index/projects.index.component';
import {
  MatCardModule,
  MatIconModule,
  MatButtonModule,
  MatInputModule,
  MatChipsModule,
  MatProgressSpinnerModule
} from '@angular/material';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    ProjectsComponent,
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatIconModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    ProjectsRoutingModule
  ]
})
export class ProjectsModule { }
