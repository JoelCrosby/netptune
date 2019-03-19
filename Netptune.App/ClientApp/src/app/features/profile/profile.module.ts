import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProfileRoutingModule } from './profile-routing.module';
import { SharedModule } from '@app/shared/shared.module';

import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ProfileComponent } from './index/profile.index.component';

@NgModule({
  declarations: [ProfileComponent],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, SharedModule, ProfileRoutingModule],
})
export class ProfileModule {}
