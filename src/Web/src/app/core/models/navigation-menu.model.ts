export interface NavigationMenuItem {
  key: string;
  label: string;
  route: string | null;
  children: NavigationMenuItem[];
}
