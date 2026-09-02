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
      body: 'Valida ràpidament el context de cada lloc des del mapa, el detall i les dades del catàleg.'
    },
    {
      badge: '03',
      body: 'Guarda favorits i reprèn la revisió més tard sense haver de tornar a començar la cerca.'
    }
  ];

  protected readonly faqs = [
    {
      badge: 'Accés',
      body: 'L’accés és amb sessió real: login, perfil i permisos segons el rol (USER, ADMIN, VIEWER).'
    },
    {
      badge: 'Favorits',
      body: 'Els favorits es desen al backend: els pots guardar mentre explores i revisar-los després amb filtres i mapa.'
    },
    {
      badge: 'Mapa',
      body: 'El mapa és part funcional de la cerca i es manté sincronitzat amb els filtres i el llistat.'
    }
  ];
}
