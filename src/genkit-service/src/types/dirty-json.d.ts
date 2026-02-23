declare module 'dirty-json' {
  export interface DirtyJson {
    parse(text: string): unknown;
  }

  const djson: DirtyJson;
  export default djson;
}
