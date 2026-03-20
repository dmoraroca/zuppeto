import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-favorite-toggle-button',
  templateUrl: './favorite-toggle-button.component.html',
  styleUrl: './favorite-toggle-button.component.scss'
})
export class FavoriteToggleButtonComponent {
  readonly active = input(false);
  readonly compact = input(false);
  readonly toggled = output<void>();

  protected onToggle(event: Event): void {
    event.stopPropagation();
    event.preventDefault();
    this.toggled.emit();
  }
}
