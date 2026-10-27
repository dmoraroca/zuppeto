export function requiredEnvironment(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Falta la variable d’entorn obligatòria ${name}.`);
  }

  return value;
}
