import { Component, input } from '@angular/core';

import { HomeWhyContent } from '../models/home-content.model';

@Component({
  selector: 'app-why-yeppet-section',
  templateUrl: './why-yeppet-section.component.html',
  styleUrl: './why-yeppet-section.component.scss'
})
export class WhyYepPetSectionComponent {
  readonly content = input.required<HomeWhyContent>();
}
