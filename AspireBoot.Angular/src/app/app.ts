import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ApplicationLayout } from './layout/application-layout';

@Component({
  imports: [ApplicationLayout],
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<app-layout />',
})
export class App {}
