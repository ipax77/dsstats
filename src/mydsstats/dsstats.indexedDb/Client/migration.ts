export type Dump = {
    __meta: { dbVersion: number; date: string };
    stores: Record<string, unknown[]>;
};

export type Migration = {
  schema?: (db: IDBDatabase, tx: IDBTransaction) => void;
  data?: (dump: Dump) => Dump;
};

