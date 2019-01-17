import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProfileRoutingModule } from './profile-routing.module';

import { ProfileComponent } from './index/profile.index.component';
import {
  MatCardModule,
  MatIconModule,
  MatButtonModule,
  MatFormFieldModule,
  MatInputModule
} from '@angular/material';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    ProfileComponent,
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatIconModule,
    MatInputModule,
    MatButtonModule,
    MatFormFieldModule,
    ProfileRoutingModule
  ]
})
export class ProfileModule { }
