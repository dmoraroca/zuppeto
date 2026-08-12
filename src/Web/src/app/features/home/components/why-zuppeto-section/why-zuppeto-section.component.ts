import { Component, input, ChangeDetectionStrategy } from '@angular/core';

import { GenericInfoCardComponent } from '../../../../shared/components/generic-info-card/generic-info-card.component';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import { HomeWhyContent } from '../../models/home-content.model';

@Component({
  selector: 'app-why-zuppeto-section',
  imports: [GenericInfoCardComponent, SectionHeadingComponent],
  templateUrl: './why-zuppeto-section.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './why-zuppeto-section.component.scss'
})
export class WhyZuppetoSectionComponent {
  readonly content = input.required<HomeWhyContent>();
}
