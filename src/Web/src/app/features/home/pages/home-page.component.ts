import { Component } from '@angular/core';

import { SiteFooterComponent } from '../../../core/layout/components/site-footer.component';
import { SiteHeaderComponent } from '../../../core/layout/components/site-header.component';
import { HomeHeroSectionComponent } from '../components/home-hero-section.component';
import { TrendingCitiesSectionComponent } from '../components/trending-cities-section.component';
import { WhyYepPetSectionComponent } from '../components/why-yeppet-section.component';
import {
  HOME_HERO_FAKE,
  HOME_TRENDING_CITIES_FAKE,
  HOME_WHY_FAKE
} from '../mock/home-page.fake';

@Component({
  selector: 'app-home-page',
  imports: [
    SiteHeaderComponent,
    SiteFooterComponent,
    HomeHeroSectionComponent,
    TrendingCitiesSectionComponent,
    WhyYepPetSectionComponent
  ],
  templateUrl: './home-page.component.html',
  styleUrl: './home-page.component.scss'
})
export class HomePageComponent {
  protected readonly heroContent = HOME_HERO_FAKE;
  protected readonly cities = HOME_TRENDING_CITIES_FAKE;
  protected readonly whyContent = HOME_WHY_FAKE;
}
