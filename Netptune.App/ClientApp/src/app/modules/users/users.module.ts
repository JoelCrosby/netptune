import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UsersRoutingModule } from './users-routing.module';

import { UsersComponent } from './index/users.index.component';
import {
  MatCardModule,
  MatIconModule,
  MatButtonModule,
  MatChipsModule,
  MatProgressSpinnerModule
} from '@angular/material';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    UsersComponent,
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    UsersRoutingModule
  ]
})
export class UsersModule { }
