import type { HeifImage } from 'libheif-js';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AvatarConverter {
  async toJpeg(file: File): Promise<File> {
    const libheif = (await import('libheif-js')).default;
    const decoder = new libheif.HeifDecoder();
    const canvas = document.createElement('canvas');
    let images: HeifImage[] = [];
    try {
      images = decoder.decode(new Uint8Array(await file.arrayBuffer()));
      const image = images.find((image) => image.is_primary()) ?? images[0];
      if (!image) throw new Error('Invalid HEIC image.');
      const width = image.get_width();
      const height = image.get_height();
      if (width <= 0 || height <= 0 || width * height > 50_000_000)
        throw new Error('Unsupported HEIC dimensions.');
      canvas.width = width;
      canvas.height = height;
      const context = canvas.getContext('2d');
      if (!context) throw new Error('Image conversion unavailable.');
      const pixels = context.createImageData(width, height);
      await new Promise<void>((resolve, reject) =>
        image.display(pixels, (data) => {
          if (data) resolve();
          else reject(new Error('HEIC decoding failed.'));
        }),
      );
      context.putImageData(pixels, 0, 0);
      const jpeg = await new Promise<Blob>((resolve, reject) =>
        canvas.toBlob(
          (blob) => {
            if (blob) resolve(blob);
            else reject(new Error('JPEG conversion failed.'));
          },
          'image/jpeg',
          0.85,
        ),
      );
      return new File([jpeg], file.name.replace(/\.[^/.]+$/, '') + '.jpg', { type: 'image/jpeg' });
    } finally {
      for (const image of images) image.free();
      if (decoder.decoder !== null) libheif.heif_context_free(decoder.decoder);
      canvas.width = 0;
      canvas.height = 0;
    }
  }
}
