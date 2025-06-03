import { StyleRenderer } from '@alyle/ui';
import {
  ImgCropperConfig,
  ImgCropperEvent,
  LyImageCropper,
} from '@alyle/ui/image-cropper';
import { Platform } from '@angular/cdk/platform';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { dataURItoBlob } from '@core/util/blob';

@Component({
    selector: 'app-image-cropper',
    templateUrl: './image-cropper.component.html',
    styleUrls: ['./image-cropper.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class ImageCropperComponent implements OnInit, AfterViewInit {
  @Input() src!: string;
  @Input() size!: number;

  @Output() cropped = new EventEmitter<{ blob: Blob; src: string }>();
  @Output() canceled = new EventEmitter();
  @Output() cleared = new EventEmitter();

  @ViewChild(LyImageCropper, { static: true })
  readonly cropper!: LyImageCropper;

  croppedImage?: string;
  scale!: number;
  ready!: boolean;
  minScale!: number;

  myConfig!: ImgCropperConfig;

  constructor(
    readonly sRenderer: StyleRenderer,
    private platform: Platform,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.myConfig = {
      width: this.size,
      height: this.size,
      type: 'image/png',
      round: true,
    };
  }

  ngAfterViewInit() {
    if (!this.platform.isBrowser) {
      return;
    }

    if (!this.src) {
      this.ready = true;
      this.cd.detectChanges();
      return;
    }

    this.cropper.loadImage(this.src);

    this.ready = true;
  }

  onCropped(e: ImgCropperEvent) {
    this.croppedImage = e.dataURL;
  }

  onCropClicked() {
    this.cropper.crop();

    const src = this.croppedImage;

    if (!src) return;

    const blob = dataURItoBlob(src);

    this.cropped.emit({ blob, src });
  }

  onCancelClicked() {
    this.canceled.emit();
  }

  onClearClicked() {
    this.cleared.emit();
  }
}
