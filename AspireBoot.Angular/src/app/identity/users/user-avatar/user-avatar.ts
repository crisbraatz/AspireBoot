import { ChangeDetectionStrategy, Component, computed, effect, input, signal } from '@angular/core';

@Component({
  selector: 'app-user-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="grid shrink-0 place-items-center overflow-hidden rounded-full border border-line bg-subtle font-semibold text-accent"
      [class.size-12]="size() === 'small'"
      [class.text-sm]="size() === 'small'"
      [class.size-28]="size() === 'large'"
      [class.text-3xl]="size() === 'large'"
    >
      @if (src() && !failed()) {
        <img
          [src]="src()"
          alt=""
          class="size-full object-cover"
          [attr.loading]="size() === 'small' ? 'lazy' : 'eager'"
          referrerpolicy="no-referrer"
          (error)="failed.set(true)"
        />
      } @else {
        <span aria-hidden="true">{{ initials() }}</span>
      }
    </span>
  `,
})
export class UserAvatar {
  readonly name = input.required<string>();
  readonly src = input<string>();
  readonly size = input<'small' | 'large'>('small');
  protected readonly failed = signal(false);
  protected readonly initials = computed(() =>
    this.name()
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => Array.from(part)[0] ?? '')
      .join('')
      .toLocaleUpperCase(),
  );

  constructor() {
    effect(() => {
      this.src();
      this.failed.set(false);
    });
  }
}
