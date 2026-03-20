import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { HomeHeroContent } from '../../models/home-content.model';

@Component({
  selector: 'app-home-hero-section',
  imports: [RouterLink],
  templateUrl: './home-hero-section.component.html',
  styleUrl: './home-hero-section.component.scss'
})
export class HomeHeroSectionComponent {
  readonly content = input.required<HomeHeroContent>();

  protected getChipQueryParams(chip: string): Record<string, string> {
    switch (chip) {
      case 'Gossos benvinguts':
        return { pet: 'dogs' };
      case 'Gats benvinguts':
        return { pet: 'cats' };
      case 'Terrassa exterior':
        return { search: 'terrassa' };
      case 'Estades llargues':
        return { type: 'apartment' };
      default:
        return {};
    }
  }
}
