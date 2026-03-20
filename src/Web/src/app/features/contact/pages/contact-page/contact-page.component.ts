import { Component } from '@angular/core';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { GenericInfoCardComponent } from '../../../../shared/components/generic-info-card/generic-info-card.component';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';

@Component({
  selector: 'app-contact-page',
  imports: [SiteHeaderComponent, SiteFooterComponent, SectionHeadingComponent, GenericInfoCardComponent],
  templateUrl: './contact-page.component.html',
  styleUrl: './contact-page.component.scss'
})
export class ContactPageComponent {
  protected readonly channels = [
    {
      title: 'Col·laboracions',
      body: 'Per partnerships, contingut o noves ciutats: hola@yeppet.fake'
    },
    {
      title: 'Suport de producte',
      body: 'Per dubtes de navegació o feedback de la fase I: suport@yeppet.fake'
    },
    {
      title: 'Horari orientatiu',
      body: 'Resposta simulada en dies laborables de 9:00 a 18:00 CET.'
    }
  ];
}
