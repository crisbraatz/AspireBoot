import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { CurrentSession } from '../identity/sessions/get-current-session/current-session';
import { Localization } from '../shared/localization/localization';
import { Appearance } from '../shared/preferences/appearance';
import { Icon } from '../shared/ui/icon';
import { Notice } from '../shared/ui/notice';

@Component({
  selector: 'app-layout',
  imports: [Icon, Notice, RouterLink, RouterLinkActive, RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './application-layout.html',
})
export class ApplicationLayout {
  protected readonly appearance = inject(Appearance);
  protected readonly i18n = inject(Localization);
  protected readonly session = inject(CurrentSession);
  protected readonly year = new Date().getFullYear();
  protected readonly menuOpen = signal(false);
  protected readonly accountAccess = signal(inject(Router).url.startsWith('/auth/'));

  constructor() {
    const document = inject(DOCUMENT);
    inject(Router)
      .events.pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe((event) => {
        this.menuOpen.set(false);
        this.accountAccess.set(event.urlAfterRedirects.split('?')[0].startsWith('/auth/'));
        document.getElementById('main-content')?.focus({ preventScroll: true });
      });
  }

  protected changeLocale(event: Event): void {
    this.i18n.setLocale((event.target as HTMLSelectElement).value);
  }

  protected closeMenu(toggle: HTMLButtonElement): void {
    if (!this.menuOpen()) return;
    this.menuOpen.set(false);
    toggle.focus();
  }
}
