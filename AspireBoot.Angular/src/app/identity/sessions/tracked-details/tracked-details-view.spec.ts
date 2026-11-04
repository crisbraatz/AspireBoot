import { TestBed } from '@angular/core/testing';
import { TrackedDetailsView } from './tracked-details-view';

describe('TrackedDetailsView', () => {
  beforeEach(() => {
    document.documentElement.lang = 'en-US';
  });

  it('should display browser, operating system, device and IP', () => {
    const fixture = TestBed.createComponent(TrackedDetailsView);
    fixture.componentRef.setInput('trackedDetails', {
      browser: 'Edge 130',
      operatingSystem: 'Windows',
      device: 'Desktop',
      remoteIpAddress: '203.0.113.7',
    });
    fixture.detectChanges();
    for (const value of ['Edge 130', 'Windows', 'Computer', '203.0.113.7'])
      expect(fixture.nativeElement.textContent).toContain(value);
  });

  it('should render untrusted device labels as text and support older sessions', () => {
    const fixture = TestBed.createComponent(TrackedDetailsView);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Unknown');
    expect(fixture.nativeElement.textContent).toContain('Unavailable');
    fixture.componentRef.setInput('trackedDetails', {
      browser: '<img src=x onerror=alert(1)>',
      operatingSystem: 'macOS',
      device: 'Desktop',
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('<img src=x onerror=alert(1)>');
  });
});
