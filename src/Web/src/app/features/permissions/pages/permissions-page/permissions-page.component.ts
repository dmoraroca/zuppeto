import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

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
  private readonly route = inject(ActivatedRoute);

  protected readonly eyebrow = this.route.snapshot.data['eyebrow'] as string ?? 'Admin';
  protected readonly title =
    (this.route.snapshot.data['title'] as string | undefined) ??
    'Vista separada per informació de sistema, accés i capes internes.';
  protected readonly copy =
    (this.route.snapshot.data['copy'] as string | undefined) ??
    'Aquesta pàgina recull la informació que no ha d’aparèixer a la portada principal i que més endavant es pot governar per permisos.';
  protected readonly sections =
    (this.route.snapshot.data['sections'] as { title: string; body: string }[] | undefined) ??
    [
      {
        title: 'Arquitectura clara',
        body: '`core`, `shared` i `features` des del primer dia per mantenir el frontend net i escalable.'
      },
      {
        title: 'Transicio a serveis reals',
        body: 'La UI ja combina persistencia real per `places`, `favorites` i perfil, mantenint una transicio progressiva i controlada cap a backend complet.'
      },
      {
        title: 'Accés per permisos',
        body: 'Aquesta capa no forma part de la portada pública. S’exposa com una vista separada i orientada a accessos controlats.'
      }
    ];
}
