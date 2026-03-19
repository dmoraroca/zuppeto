import { Component, input } from '@angular/core';

import { HomeHeroContent } from '../models/home-content.model';

@Component({
  selector: 'app-home-hero-section',
  templateUrl: './home-hero-section.component.html',
  styleUrl: './home-hero-section.component.scss'
})
export class HomeHeroSectionComponent {
  readonly content = input.required<HomeHeroContent>();
}
