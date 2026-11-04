import { TestBed } from '@angular/core/testing';
import { AvatarConverter } from './avatar-converter';

const decoder = vi.hoisted(() => ({ decode: vi.fn(), free: vi.fn() }));
vi.mock('libheif-js', () => ({
  default: {
    HeifDecoder: class {
      decoder = 1;
      decode = decoder.decode;
    },
    heif_context_free: decoder.free,
  },
}));

describe('AvatarConverter', () => {
  const image = {
    display: vi.fn(),
    free: vi.fn(),
    get_height: vi.fn(),
    get_width: vi.fn(),
    is_primary: vi.fn(),
  };
  beforeEach(() => {
    decoder.free.mockReset();
    decoder.decode.mockReset().mockReturnValue([image]);
    image.free.mockReset();
    image.get_width.mockReset().mockReturnValue(1);
    image.get_height.mockReset().mockReturnValue(1);
    image.is_primary.mockReset().mockReturnValue(true);
    image.display.mockReset().mockImplementation((pixels, callback) => callback(pixels));
    vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({
      createImageData: () => ({ width: 1, height: 1, data: new Uint8ClampedArray(4) }),
      putImageData: vi.fn(),
    } as unknown as CanvasRenderingContext2D);
    vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation((callback) =>
      callback(new Blob(['jpeg'], { type: 'image/jpeg' })),
    );
  });
  afterEach(() => vi.restoreAllMocks());

  it('should encode the decoded photo as JPEG and release decoder resources', async () => {
    const file = new File(['heic'], 'photo.HEIC', { type: 'image/heic' });
    Object.defineProperty(file, 'arrayBuffer', { value: async () => new ArrayBuffer(4) });
    const result = await TestBed.inject(AvatarConverter).toJpeg(file);
    expect(result.name).toBe('photo.jpg');
    expect(result.type).toBe('image/jpeg');
    expect(result.size).toBe(4);
    expect(HTMLCanvasElement.prototype.toBlob).toHaveBeenCalledWith(
      expect.any(Function),
      'image/jpeg',
      0.85,
    );
    expect(image.free).toHaveBeenCalledExactlyOnceWith();
    expect(decoder.free).toHaveBeenCalledExactlyOnceWith(1);
  });

  it('should reject an invalid container without encoding it', async () => {
    decoder.decode.mockReturnValue([]);
    const file = new File(['invalid'], 'photo.heic');
    Object.defineProperty(file, 'arrayBuffer', { value: async () => new ArrayBuffer(4) });
    await expect(TestBed.inject(AvatarConverter).toJpeg(file)).rejects.toThrow(
      'Invalid HEIC image.',
    );
    expect(HTMLCanvasElement.prototype.toBlob).not.toHaveBeenCalled();
    expect(decoder.free).toHaveBeenCalledExactlyOnceWith(1);
  });

  it.each([0, 50_000_001])(
    'should bound decoded image dimensions before allocating pixels',
    async (width) => {
      image.get_width.mockReturnValue(width);
      const file = new File(['heic'], 'photo.heic');
      Object.defineProperty(file, 'arrayBuffer', { value: async () => new ArrayBuffer(4) });
      await expect(TestBed.inject(AvatarConverter).toJpeg(file)).rejects.toThrow(
        'Unsupported HEIC dimensions.',
      );
      expect(image.display).not.toHaveBeenCalled();
      expect(image.free).toHaveBeenCalledExactlyOnceWith();
      expect(decoder.free).toHaveBeenCalledExactlyOnceWith(1);
    },
  );

  it('should release resources after decoding failure', async () => {
    image.display.mockImplementation((_pixels, callback) => callback(null));
    const file = new File(['heic'], 'photo.heic');
    Object.defineProperty(file, 'arrayBuffer', { value: async () => new ArrayBuffer(4) });
    await expect(TestBed.inject(AvatarConverter).toJpeg(file)).rejects.toThrow(
      'HEIC decoding failed.',
    );
    expect(HTMLCanvasElement.prototype.toBlob).not.toHaveBeenCalled();
    expect(image.free).toHaveBeenCalledExactlyOnceWith();
    expect(decoder.free).toHaveBeenCalledExactlyOnceWith(1);
  });

  it('should reject failed JPEG encoding and release resources', async () => {
    vi.mocked(HTMLCanvasElement.prototype.toBlob).mockImplementation((callback) => callback(null));
    const file = new File(['heic'], 'photo.heic');
    Object.defineProperty(file, 'arrayBuffer', { value: async () => new ArrayBuffer(4) });
    await expect(TestBed.inject(AvatarConverter).toJpeg(file)).rejects.toThrow(
      'JPEG conversion failed.',
    );
    expect(image.free).toHaveBeenCalledExactlyOnceWith();
    expect(decoder.free).toHaveBeenCalledExactlyOnceWith(1);
  });
});
