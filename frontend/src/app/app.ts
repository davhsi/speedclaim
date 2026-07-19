import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainerComponent } from './shared/components/toast/toast-container';
import { SpeedyAssistantComponent } from './features/portal/speedy-assistant/speedy-assistant';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastContainerComponent, SpeedyAssistantComponent],
  templateUrl: './app.html',
})
export class App {}
