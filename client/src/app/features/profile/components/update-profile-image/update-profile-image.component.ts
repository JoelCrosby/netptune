import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-update-profile-image',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [StrokedButtonComponent],
  template: `<div class="w-full max-[1036px]:mb-16 max-[1036px]:max-w-[480px]">
    <div class="mx-auto my-[1.4rem] flex w-[180px] flex-col items-center">
      <img
        crossorigin="anonymous"
        class="mx-auto h-[180px] w-[180px] rounded-full object-cover"
        [src]="
          pictureUrl() ||
          'https://netptune.s3.eu-west-2.amazonaws.com/common/placeholder/no_profile.png'
        "
        alt="Profile Image"
        height="180"
        width="180" />
      <button
        class="mx-auto my-[1.4rem]"
        app-stroked-button
        (click)="$event.preventDefault(); changePictureClicked.emit()">
        Change Picture
      </button>
    </div>
  </div> `,
})
export class UpdateProfileImageComponent {
  pictureUrl = input<string>('');
  changePictureClicked = output();
}
