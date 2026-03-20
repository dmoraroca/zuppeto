import { Component, input } from '@angular/core';

import { GenericInfoCardComponent } from '../../../../shared/components/generic-info-card/generic-info-card.component';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import { HomeWhyContent } from '../../models/home-content.model';

@Component({
  selector: 'app-why-yeppet-section',
  imports: [GenericInfoCardComponent, SectionHeadingComponent],
  templateUrl: './why-yeppet-section.component.html',
  styleUrl: './why-yeppet-section.component.scss'
})
export class WhyYepPetSectionComponent {
  readonly content = input.required<HomeWhyContent>();
}
