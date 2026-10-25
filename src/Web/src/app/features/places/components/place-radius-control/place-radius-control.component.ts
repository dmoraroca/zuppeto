import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal } from '@angular/core';

import { PlaceSearchRadius } from '../../models/place.model';

const DISTANCE_RADII = [5, 10, 20, 30, 50] as const;

@Component({
  selector: 'app-place-radius-control',
  standalone: true,
  templateUrl: './place-radius-control.component.html',
  styleUrl: './place-radius-control.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlaceRadiusControlComponent {
  readonly value = input<PlaceSearchRadius>(0);
  readonly disabled = input(false);
  readonly valueConfirmed = output<PlaceSearchRadius>();

  protected readonly radii = DISTANCE_RADII;
  protected readonly preview = signal<Exclude<PlaceSearchRadius, 0>>(10);
  protected readonly previewIndex = computed(() => DISTANCE_RADII.indexOf(this.preview()));
  protected readonly needleAngle = computed(() => -90 + this.previewIndex() * 45);

  constructor() {
    effect(() => {
      const current = this.value();
      if (current !== 0) this.preview.set(current);
    });
  }

  protected previewRadius(event: Event): void {
    if (this.disabled()) return;
    const index = Number((event.target as HTMLInputElement).value);
    this.preview.set(DISTANCE_RADII[index] ?? 10);
  }

  protected confirmRadius(event: Event): void {
    if (this.disabled()) return;
    const index = Number((event.target as HTMLInputElement).value);
    const radius = DISTANCE_RADII[index];
    if (radius !== undefined && radius !== this.value()) this.valueConfirmed.emit(radius);
  }

  protected selectMunicipality(): void {
    if (this.disabled()) return;
    if (this.value() !== 0) this.valueConfirmed.emit(0);
  }
}
