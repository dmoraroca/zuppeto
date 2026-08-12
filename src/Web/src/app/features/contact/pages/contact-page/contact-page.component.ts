import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { GenericInfoCardComponent } from '../../../../shared/components/generic-info-card/generic-info-card.component';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';

@Component({
  selector: 'app-contact-page',
  imports: [
    RouterLink,
    SiteHeaderComponent,
    SiteFooterComponent,
    SectionHeadingComponent,
    GenericInfoCardComponent
  ],
  templateUrl: './contact-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './contact-page.component.scss'
})
export class ContactPageComponent {
  protected readonly channels = [
    {
      title: 'Col·laboracions',
      body: 'Per col·laboracions, contingut o noves ciutats: partnerships@zuppeto.fake'
    },
    {
      title: 'Suport de producte',
      body: 'Per dubtes de navegació, favorits, perfil o cerca: suport@zuppeto.fake'
    },
    {
      title: 'Temps de resposta',
      body: 'Resposta simulada en 24-48 h laborables per validar el flux informatiu del producte.'
    }
  ];
}
