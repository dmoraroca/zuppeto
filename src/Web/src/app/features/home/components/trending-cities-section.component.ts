import { Component, input } from '@angular/core';

import { HomeCity } from '../models/home-content.model';

@Component({
  selector: 'app-trending-cities-section',
  templateUrl: './trending-cities-section.component.html',
  styleUrl: './trending-cities-section.component.scss'
})
export class TrendingCitiesSectionComponent {
  readonly cities = input.required<HomeCity[]>();
}
