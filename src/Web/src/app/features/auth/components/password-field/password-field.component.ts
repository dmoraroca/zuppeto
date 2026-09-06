import { Component, computed, effect, inject, input, output, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

import {
  PASSWORD_STRENGTH_POLICY,
  PasswordStrengthPolicy
} from '../../policies/password-strength.policy';

@Component({
  selector: 'app-password-field',
  imports: [ReactiveFormsModule],
  templateUrl: './password-field.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './password-field.component.scss'
})
export class PasswordFieldComponent {
  private readonly strengthPolicy = inject<PasswordStrengthPolicy>(PASSWORD_STRENGTH_POLICY);

  readonly label = input.required<string>();
  readonly control = input.required<FormControl<string>>();
  readonly required = input(false);
  readonly autocomplete = input('current-password');
  readonly showStrength = input(false);
  readonly errorMessage = input('');
  readonly matchControl = input<FormControl<string> | null>(null);
  readonly locked = input(false);
  readonly blockAutofill = input(false);
  readonly valueChanged = output<void>();
  readonly focused = output<void>();

  protected readonly visible = signal(false);
  private readonly hasFocus = signal(false);
  private readonly valueTick = signal('');
  private readonly matchTick = signal('');

  protected readonly isLocked = computed(() => this.locked() || this.control().disabled);

  protected readonly isReadOnly = computed(
    () => this.isLocked() || (this.blockAutofill() && !this.hasFocus())
  );

  constructor() {
    effect((onCleanup) => {
      const control = this.control();
      this.valueTick.set(control.value);
      const subscription = control.valueChanges.subscribe((value) => {
        this.valueTick.set(value);
        this.valueChanged.emit();
      });
      onCleanup(() => subscription.unsubscribe());
    });

    effect((onCleanup) => {
      const match = this.matchControl();
      if (!match) {
        this.matchTick.set('');
        return;
      }

      this.matchTick.set(match.value);
      const subscription = match.valueChanges.subscribe((value) => this.matchTick.set(value));
      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected readonly fieldError = computed(() => {
    if (this.errorMessage()) {
      return this.errorMessage();
    }

    if (!this.matchControl()) {
      return '';
    }

    const confirm = this.valueTick().trim();
    const other = this.matchTick().trim();
    if (!confirm && !other) {
      return '';
    }

    return confirm !== other ? 'Les contrasenyes no coincideixen.' : '';
  });

  protected readonly strength = computed(() => this.strengthPolicy.evaluate(this.valueTick()));

  protected readonly strengthLabel = computed(() => {
    switch (this.strength()) {
      case 'strong':
        return 'Forta';
      case 'medium':
        return 'Mitjana';
      case 'weak':
        return 'Dèbil';
      default:
        return '';
    }
  });

  protected onFocus(): void {
    this.hasFocus.set(true);
    this.focused.emit();
  }

  protected onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (this.isLocked()) {
      input.value = this.control().value;
      return;
    }

    const value = input.value;
    this.control().setValue(value, { emitEvent: true });
    this.control().markAsDirty();
    this.control().markAsTouched();
    this.valueTick.set(value);
    this.valueChanged.emit();
  }

  protected toggleVisibility(): void {
    this.visible.update((visible) => !visible);
  }
}
