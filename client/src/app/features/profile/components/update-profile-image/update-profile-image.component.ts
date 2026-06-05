import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-update-profile-image',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [StrokedButtonComponent],
  template: `<div class="w-full max-[1036px]:mb-16 max-[1036px]:max-w-120">
    <div class="mx-auto my-[1.4rem] flex w-45 flex-col items-center">
      <img
        class="mx-auto h-45 w-45 rounded-full object-cover"
        [src]="displayPicture()"
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

  defaultPicture =
    'https://netptune.s3.eu-west-2.amazonaws.com/common/placeholder/no_profile.png';

  displayPicture = computed(() => this.pictureUrl() ?? this.defaultPicture);
}
