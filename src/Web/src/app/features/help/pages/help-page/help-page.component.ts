import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { GenericInfoCardComponent } from '../../../../shared/components/generic-info-card/generic-info-card.component';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';

@Component({
  selector: 'app-help-page',
  imports: [
    RouterLink,
    SiteHeaderComponent,
    SiteFooterComponent,
    SectionHeadingComponent,
    GenericInfoCardComponent
  ],
  templateUrl: './help-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './help-page.component.scss'
})
export class HelpPageComponent {
  protected readonly steps = [
    {
      badge: '01',
      body: 'Entra a llocs, aplica filtres útils i treballa amb una sola pantalla on conviuen mapa i llistat.'
    },
    {
      badge: '02',
      body: 'Valida ràpidament el context de cada lloc des del mapa, el detall i les dades fake enriquides.'
    },
    {
      badge: '03',
      body: 'Guarda favorits i reprèn la revisió més tard sense haver de tornar a començar la cerca.'
    }
  ];

  protected readonly faqs = [
    {
      badge: 'Accés',
      body: 'Zuppeto treballa ara mateix amb login fake per validar navegació, perfil i accessos per rol.'
    },
    {
      badge: 'Favorits',
      body: 'Els favorits encara no són persistits a backend, però ja simulen el flux real de guardar i revisar.'
    },
    {
      badge: 'Mapa',
      body: 'El mapa és part funcional de la cerca i es manté sincronitzat amb els filtres i el llistat.'
    }
  ];
}
