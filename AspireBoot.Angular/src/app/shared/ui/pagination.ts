import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { Localization } from '../localization/localization';
import { Icon } from './icon';

@Component({
  selector: 'app-pagination',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav
      class="flex flex-wrap items-center justify-between gap-4 border-t border-line px-5 py-4"
      [attr.aria-label]="i18n.t('users.page', { page: page(), total: total() })"
    >
      <label class="flex items-center gap-2 text-sm text-muted"
        >{{ i18n.t('users.pageSize') }}
        <span class="relative">
          <select
            class="select-control rounded-lg text-ink"
            [value]="size()"
            [disabled]="disabled()"
            (change)="resize($event)"
          >
            @if (!sizes.includes(size())) {
              <option [value]="size()">{{ size() }}</option>
            }
            @for (value of sizes; track value) {
              <option [value]="value">{{ value }}</option>
            }
          </select>
          <app-icon
            name="chevron-down"
            class="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-muted"
          />
        </span>
      </label>
      <div class="flex items-center gap-3">
        <span class="text-xs text-muted">{{
          i18n.t('users.page', { page: page(), total: total() })
        }}</span>
        <button
          type="button"
          class="icon-button"
          [disabled]="disabled() || page() <= 1"
          [attr.aria-label]="i18n.t('users.previous')"
          (click)="pageChange.emit({ page: page() - 1, size: size() })"
        >
          <app-icon name="back" />
        </button>
        <button
          type="button"
          class="icon-button"
          [disabled]="disabled() || page() >= total()"
          [attr.aria-label]="i18n.t('users.next')"
          (click)="pageChange.emit({ page: page() + 1, size: size() })"
        >
          <app-icon name="next" />
        </button>
      </div>
    </nav>
  `,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly size = input.required<number>();
  readonly count = input.required<number>();
  readonly disabled = input(false);
  readonly pageChange = output<{ page: number; size: number }>();
  protected readonly i18n = inject(Localization);
  protected readonly sizes = [10, 20, 50, 100];
  protected readonly total = computed(() => Math.max(1, Math.ceil(this.count() / this.size())));

  protected resize(event: Event): void {
    this.pageChange.emit({ page: 1, size: Number((event.target as HTMLSelectElement).value) });
  }
}
