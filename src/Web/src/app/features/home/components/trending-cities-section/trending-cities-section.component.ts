import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import { HomeCity } from '../../models/home-content.model';

@Component({
  selector: 'app-trending-cities-section',
  imports: [RouterLink, SectionHeadingComponent],
  templateUrl: './trending-cities-section.component.html',
  styleUrl: './trending-cities-section.component.scss'
})
export class TrendingCitiesSectionComponent {
  readonly cities = input.required<HomeCity[]>();
}
