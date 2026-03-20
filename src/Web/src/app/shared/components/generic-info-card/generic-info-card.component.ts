import { Component, input } from '@angular/core';

@Component({
  selector: 'app-generic-info-card',
  templateUrl: './generic-info-card.component.html',
  styleUrl: './generic-info-card.component.scss'
})
export class GenericInfoCardComponent {
  readonly badge = input<string>();
  readonly body = input.required<string>();
}
