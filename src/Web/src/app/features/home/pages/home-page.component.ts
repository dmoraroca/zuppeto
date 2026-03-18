import { Component } from '@angular/core';

import { SiteFooterComponent } from '../../../core/layout/components/site-footer.component';
import { SiteHeaderComponent } from '../../../core/layout/components/site-header.component';

@Component({
  selector: 'app-home-page',
  imports: [SiteHeaderComponent, SiteFooterComponent],
  templateUrl: './home-page.component.html',
  styleUrl: './home-page.component.scss'
})
export class HomePageComponent {
  protected readonly categories = [
    'Dogs welcome',
    'Cats welcome',
    'Outdoor friendly',
    'Long stays'
  ];

  protected readonly cities = [
    {
      name: 'Barcelona',
      country: 'Spain',
      vibe: 'Terraces, urban walks and weekend escapes'
    },
    {
      name: 'Madrid',
      country: 'Spain',
      vibe: 'Hotels, brunch spots and dog-friendly parks'
    },
    {
      name: 'Lisboa',
      country: 'Portugal',
      vibe: 'Sunny stays and relaxed neighborhoods'
    },
    {
      name: 'Berlin',
      country: 'Germany',
      vibe: 'Independent cafes and pet-friendly apartments'
    }
  ];

  protected readonly reasons = [
    'No més trucades per confirmar si accepten mascotes.',
    'Filtres pensats per la vida real, no per fer bonic.',
    'Una base preparada per créixer cap a comunitat i serveis.'
  ];
}
