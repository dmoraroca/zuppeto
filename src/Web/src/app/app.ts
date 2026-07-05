import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AppToastStackComponent } from './core/layout/components/app-toast-stack/app-toast-stack.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AppToastStackComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {}
