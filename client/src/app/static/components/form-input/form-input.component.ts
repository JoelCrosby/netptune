import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-form-input',
  templateUrl: './form-input.component.html',
  styleUrls: ['./form-input.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormInputComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }

}
