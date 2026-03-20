import { Component } from '@angular/core';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { GenericInfoCardComponent } from '../../../../shared/components/generic-info-card/generic-info-card.component';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';

@Component({
  selector: 'app-permissions-page',
  imports: [SiteHeaderComponent, SiteFooterComponent, GenericInfoCardComponent, SectionHeadingComponent],
  templateUrl: './permissions-page.component.html',
  styleUrl: './permissions-page.component.scss'
})
export class PermissionsPageComponent {
  protected readonly sections = [
    {
      title: 'Arquitectura clara',
      body: '`core`, `shared` i `features` des del primer dia per mantenir el frontend net i escalable.'
    },
    {
      title: 'Dades mock-first',
      body: 'La UI treballa sobre serveis mock substituibles més endavant per `HttpClient` i backend real.'
    },
    {
      title: 'Accés per permisos',
      body: 'Aquesta capa no forma part de la portada pública. S’exposa com una vista separada i orientada a accessos controlats.'
    }
  ];
}
