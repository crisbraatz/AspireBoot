import { ChangeDetectionStrategy, Component, input } from '@angular/core';

const paths = {
  menu: ['M4 6h16M4 12h16M4 18h16'],
  close: ['m6 6 12 12M6 18 18 6'],
  'chevron-down': ['m6 9 6 6 6-6'],
  login: ['M10 17l5-5-5-5', 'M15 12H3', 'M12 3h7a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-7'],
  logout: ['M14 17l5-5-5-5', 'M19 12H7', 'M10 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h5'],
  'user-plus': [
    'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2',
    'M9 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8',
    'M20 8v6m-3-3h6',
  ],
  users: [
    'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2',
    'M9 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8',
    'M17 3a4 4 0 0 1 0 8m5 10v-2a4 4 0 0 0-3-3.87',
  ],
  user: ['M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2', 'M12 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8'],
  back: ['M19 12H5m7-7-7 7 7 7'],
  next: ['M5 12h14m-7-7 7 7-7 7'],
  edit: ['M12 20H4V12', 'M15 5l4 4', 'M8 16l1-5L18 2l4 4-9 9-5 1Z'],
  shield: ['M12 22s8-4 8-11V5l-8-3-8 3v6c0 7 8 11 8 11Z', 'm9 12 2 2 4-4'],
  save: [
    'M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h12l4 4v12a2 2 0 0 1-2 2Z',
    'M7 3v6h10V3M7 21v-8h10v8',
  ],
  mail: [
    'M4 4h16a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2Z',
    'm22 6-10 7L2 6',
  ],
  trash: ['M3 6h18M9 6V4h6v2M5 6l1 15h12l1-15M10 10v7M14 10v7'],
  key: ['M8 15a5 5 0 1 1 4-8l9 9v5h-5v-3h-3l-3-3', 'M7 7h.01'],
} as const;

@Component({
  selector: 'app-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex shrink-0', 'aria-hidden': 'true' },
  template: `
    <svg
      width="18"
      height="18"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.7"
      stroke-linecap="round"
      stroke-linejoin="round"
      focusable="false"
    >
      @for (path of paths[name()]; track $index) {
        <path [attr.d]="path" />
      }
    </svg>
  `,
})
export class Icon {
  readonly name = input.required<keyof typeof paths>();
  protected readonly paths = paths;
}
