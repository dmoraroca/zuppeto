export interface BrowserTarget {
  readonly key: 'chrome' | 'firefox';
  readonly name: 'Chrome' | 'Firefox';
  readonly engine: 'Blink' | 'Gecko';
  readonly runsDirectoryName?: string;
}

export const chromeTarget: BrowserTarget = { key: 'chrome', name: 'Chrome', engine: 'Blink' };
export const firefoxTarget: BrowserTarget = { key: 'firefox', name: 'Firefox', engine: 'Gecko', runsDirectoryName: 'firefox' };
