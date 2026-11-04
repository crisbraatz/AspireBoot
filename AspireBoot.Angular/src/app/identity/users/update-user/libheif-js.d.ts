declare module 'libheif-js' {
  export interface HeifImage {
    display(data: ImageData, callback: (data: ImageData | null) => void): void;
    free(): void;
    get_height(): number;
    get_width(): number;
    is_primary(): boolean;
  }

  const libheif: {
    HeifDecoder: new () => { decoder: number | null; decode(data: Uint8Array): HeifImage[] };
    heif_context_free(context: number): void;
  };
  export default libheif;
}
