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
  protected readonly categories = ['Restaurants', 'Hotels', 'Apartments', 'Parks', 'Services'];
}
