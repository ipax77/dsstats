declare const pako: {
    gzip(data: string | Uint8Array, options?: any): Uint8Array<ArrayBuffer>;

    // overload: when { to: "string" } is passed, return string
    ungzip(data: Uint8Array<ArrayBuffer>, options: { to: "string" }): string;

    // fallback: otherwise return Uint8Array
    ungzip(data: Uint8Array<ArrayBuffer>, options?: any): Uint8Array<ArrayBuffer>;
};