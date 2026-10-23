import { AfterViewInit, Component, ElementRef, HostListener, inject, input, output, ViewChild } from '@angular/core';

import { PlaceShareResult, PlaceShareService, PlaceShareTarget } from '../../services/place-share.service';

@Component({
  selector: 'app-place-share-menu',
  templateUrl: './place-share-menu.component.html',
  styleUrl: './place-share-menu.component.scss'
})
export class PlaceShareMenuComponent implements AfterViewInit {
  private readonly shareService = inject(PlaceShareService);
  public readonly target = input.required<PlaceShareTarget>();
  public readonly closed = output<void>();
  public readonly feedback = output<string>();
  protected busy = false;

  @ViewChild('dialog', { read: ElementRef })
  private readonly dialog?: ElementRef<HTMLElement>;

  public ngAfterViewInit(): void {
    this.dialog?.nativeElement.querySelector<HTMLElement>('[autofocus]')?.focus();
  }

  protected whatsappUrl(): string { return this.shareService.buildWhatsAppUrl(this.target()); }
  protected telegramUrl(): string { return this.shareService.buildTelegramUrl(this.target()); }
  protected mailtoUrl(): string { return this.shareService.buildMailtoUrl(this.target()); }
  protected canShareNatively(): boolean { return this.shareService.canShareNatively(); }

  protected close(): void { this.closed.emit(); }

  protected async copyLink(): Promise<void> {
    if (this.busy) return;
    this.busy = true;
    try {
      this.handleResult(await this.shareService.copy(this.target()));
    } finally {
      this.busy = false;
    }
  }

  protected async moreOptions(): Promise<void> {
    if (this.busy) return;
    this.busy = true;
    try {
      this.handleResult(await this.shareService.shareNatively(this.target()));
    } finally {
      this.busy = false;
    }
  }

  protected handleBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.close();
  }

  @HostListener('document:keydown', ['$event'])
  protected handleKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
      return;
    }
    if (event.key !== 'Tab') return;
    const focusable = Array.from(this.dialog?.nativeElement.querySelectorAll<HTMLElement>(
      'button:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])'
    ) ?? []);
    const first = focusable[0];
    const last = focusable.at(-1);
    if (!first || !last) return;
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  private handleResult(result: PlaceShareResult): void {
    if (result.status === 'copied') this.feedback.emit('Enllaç copiat');
    if (result.status === 'shared') this.feedback.emit('Establiment compartit.');
    if (result.status === 'manual' || result.status === 'failed') this.feedback.emit(result.message);
    if (result.status !== 'cancelled') this.close();
  }
}
