import { Component, ViewChild, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { BreakpointObserver } from '@angular/cdk/layout';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [MatSidenavModule, SidebarComponent, HeaderComponent, RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  private breakpointObserver = inject(BreakpointObserver);

  isMobile = signal(false);
  @ViewChild('sidenav') sidenav!: MatSidenav;

  constructor() {
    this.breakpointObserver
      .observe(['(max-width: 767px)'])
      .subscribe(result => this.isMobile.set(result.matches));
  }

  get sidenavMode(): 'side' | 'over' {
    return this.isMobile() ? 'over' : 'side';
  }

  get sidenavOpened(): boolean {
    return !this.isMobile();
  }

  onNavigation(): void {
    if (this.isMobile()) {
      this.sidenav.close();
    }
  }

  toggleSidenav(): void {
    this.sidenav.toggle();
  }
}
