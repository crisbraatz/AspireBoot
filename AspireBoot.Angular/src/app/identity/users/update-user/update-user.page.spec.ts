import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { CurrentUser } from '../get-current-user/current-user';
import { UsersApi } from '../users-api';
import { UpdateUserPage } from './update-user.page';
import { AvatarConverter } from './avatar-converter';

const nativeURL = URL;
describe('UpdateUserPage', () => {
  let api: { updateUser: ReturnType<typeof vi.fn> };
  let converter: { toJpeg: ReturnType<typeof vi.fn> };
  beforeEach(() => {
    converter = { toJpeg: vi.fn() };
    document.documentElement.lang = 'en-US';
    vi.stubGlobal(
      'URL',
      class extends nativeURL {
        static override createObjectURL = vi
          .fn()
          .mockReturnValueOnce('blob:first')
          .mockReturnValueOnce('blob:second');
        static override revokeObjectURL = vi.fn();
      },
    );
    api = { updateUser: vi.fn().mockReturnValue(of(undefined)) };
    TestBed.configureTestingModule({
      imports: [UpdateUserPage],
      providers: [
        provideRouter([{ path: 'profile', children: [] }]),
        { provide: CurrentUser, useValue: { user: signal({ id: 'owner', name: 'Owner Name' }) } },
        { provide: UsersApi, useValue: api },
        { provide: AvatarConverter, useValue: converter },
      ],
    });
  });
  afterEach(() => vi.unstubAllGlobals());
  it('should submit the caller profile without a target ID', async () => {
    const fixture = TestBed.createComponent(UpdateUserPage);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#profile-name') as HTMLInputElement;
    input.value = 'Updated Name';
    input.dispatchEvent(new Event('input'));
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();
    expect(api.updateUser).toHaveBeenCalledWith({ newName: 'Updated Name', avatar: null });
  });

  it('should release both replaced and remaining preview URLs', () => {
    const fixture = TestBed.createComponent(UpdateUserPage);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#avatar') as HTMLInputElement;
    Object.defineProperty(input, 'files', {
      value: [new File(['one'], 'one.png', { type: 'image/png' })],
      configurable: true,
    });
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    Object.defineProperty(input, 'files', {
      value: [new File(['two'], 'two.png', { type: 'image/png' })],
      configurable: true,
    });
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:first');
    fixture.destroy();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:second');
  });

  it('should convert HEIC before previewing and uploading a JPEG', async () => {
    const jpeg = new File(['jpeg'], 'photo.jpg', { type: 'image/jpeg' });
    converter.toJpeg.mockResolvedValue(jpeg);
    const fixture = TestBed.createComponent(UpdateUserPage);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#avatar') as HTMLInputElement;
    const heic = new File(['heic'], 'photo.HEIC', { type: 'image/heic' });
    Object.defineProperty(input, 'files', { value: [heic], configurable: true });
    input.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    expect(converter.toJpeg).toHaveBeenCalledWith(heic);
    expect(URL.createObjectURL).toHaveBeenCalledWith(jpeg);
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();
    expect(api.updateUser).toHaveBeenCalledWith({ newName: 'Owner Name', avatar: jpeg });
  });

  it('should block an invalid name before sending a request', async () => {
    const fixture = TestBed.createComponent(UpdateUserPage);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#profile-name') as HTMLInputElement;
    input.value = 'A';
    input.dispatchEvent(new Event('input'));
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();
    expect(api.updateUser).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Use at least 2 characters.');
  });

  it('should reject invalid HEIC without submitting the original bytes', async () => {
    converter.toJpeg.mockRejectedValue(new Error('Invalid HEIC'));
    const fixture = TestBed.createComponent(UpdateUserPage);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#avatar') as HTMLInputElement;
    Object.defineProperty(input, 'files', {
      value: [new File(['invalid'], 'photo.heic')],
      configurable: true,
    });
    input.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('This image could not be accepted.');
    expect(fixture.nativeElement.querySelector('button[type="submit"]').disabled).toBe(true);
    expect(URL.createObjectURL).not.toHaveBeenCalled();
    expect(api.updateUser).not.toHaveBeenCalled();
  });

  it('should ignore a late conversion after another photo is selected', async () => {
    let finish!: (file: File) => void;
    converter.toJpeg.mockReturnValue(
      new Promise((resolve) => {
        finish = resolve;
      }),
    );
    const fixture = TestBed.createComponent(UpdateUserPage);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#avatar') as HTMLInputElement;
    Object.defineProperty(input, 'files', {
      value: [new File(['heic'], 'photo.heic')],
      configurable: true,
    });
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button[type="submit"]').disabled).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Preparing your photo');
    const png = new File(['png'], 'photo.png', { type: 'image/png' });
    Object.defineProperty(input, 'files', { value: [png], configurable: true });
    input.dispatchEvent(new Event('change'));
    finish(new File(['jpeg'], 'photo.jpg', { type: 'image/jpeg' }));
    await fixture.whenStable();
    expect(URL.createObjectURL).toHaveBeenCalledExactlyOnceWith(png);
    expect(fixture.nativeElement.textContent).not.toContain('Preparing your photo');
  });

  it('should reject a converted JPEG that exceeds the upload limit', async () => {
    converter.toJpeg.mockResolvedValue(
      new File([new Uint8Array(4 * 1024 * 1024 + 1)], 'photo.jpg', { type: 'image/jpeg' }),
    );
    const fixture = TestBed.createComponent(UpdateUserPage);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#avatar') as HTMLInputElement;
    Object.defineProperty(input, 'files', {
      value: [new File(['heic'], 'photo.heic')],
      configurable: true,
    });
    input.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('no larger than 4 MiB');
    expect(URL.createObjectURL).not.toHaveBeenCalled();
    expect(api.updateUser).not.toHaveBeenCalled();
  });

  it('should retain the edit form and display server-side avatar rejection', async () => {
    api.updateUser.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { codes: { avatar: ['identity.users.invalid-avatar'] } },
          }),
      ),
    );
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');
    const fixture = TestBed.createComponent(UpdateUserPage);
    fixture.detectChanges();

    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('This image could not be accepted.');
    expect(fixture.nativeElement.querySelector('#profile-name').value).toBe('Owner Name');
    expect(fixture.nativeElement.querySelector('fieldset').disabled).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('should stop waiting for a write and not navigate after leaving the page', async () => {
    const response = new Subject<void>();
    api.updateUser.mockReturnValue(response);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');
    const fixture = TestBed.createComponent(UpdateUserPage);
    fixture.detectChanges();
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    expect(response.observed).toBe(true);
    fixture.destroy();
    expect(response.observed).toBe(false);
    response.next();
    response.complete();
    await Promise.resolve();
    expect(navigate).not.toHaveBeenCalled();
  });
});
