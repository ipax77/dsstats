import '../setup';
import { vi } from 'vitest';

vi.mock("brotli-wasm", () => ({
  default: Promise.resolve({
    compress: (content: Uint8Array, options?: { quality?: number }) => {
      if (options?.quality !== 5) {
        throw new Error(`unexpected Brotli quality ${options?.quality}`);
      }

      return new Uint8Array([7, 8, 9]);
    },
    decompress: (_content: Uint8Array) => new Uint8Array([68, 83, 80, 66]),
  }),
}));
