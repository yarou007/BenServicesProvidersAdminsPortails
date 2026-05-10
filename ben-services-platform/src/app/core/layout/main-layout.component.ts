import { BreakpointObserver } from '@angular/cdk/layout';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { AuthService } from '../services/auth.service';

interface NavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ReactiveFormsModule,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatMenuModule,
    MatBadgeModule
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MainLayoutComponent {
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  protected readonly searchControl = new FormControl('', { nonNullable: true });

  protected readonly navItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Providers', route: '/providers', icon: 'business_center' },
    { label: 'Add Provider', route: '/providers/add', icon: 'person_add' },
    { label: 'Applications', route: '/applications', icon: 'assignment' },
    { label: 'Regions / Market Analysis', route: '/regions', icon: 'location_city' },
    { label: 'Reports & Export', route: '/reports', icon: 'bar_chart' },
    { label: 'Settings', route: '/settings', icon: 'settings' }
  ];

  protected readonly isHandset = toSignal(
    this.breakpointObserver.observe('(max-width: 1023px)').pipe(map((state) => state.matches)),
    { initialValue: false }
  );

  protected readonly mobileSidebarOpen = signal(false);
  protected readonly desktopSidebarCollapsed = signal(false);

  protected readonly sidenavMode = computed(() => (this.isHandset() ? 'over' : 'side'));
  protected readonly sidenavOpened = computed(() => (this.isHandset() ? this.mobileSidebarOpen() : true));
  protected readonly isSidebarCollapsed = computed(() => !this.isHandset() && this.desktopSidebarCollapsed());
  protected readonly sidebarToggleAriaLabel = computed(() => {
    if (this.isHandset()) {
      return this.mobileSidebarOpen() ? 'Close menu' : 'Open menu';
    }

    return this.isSidebarCollapsed() ? 'Expand sidebar' : 'Collapse sidebar';
  });
  protected readonly sidebarToggleIcon = computed(() => {
    if (this.isHandset()) {
      return this.sidenavOpened() ? 'close' : 'menu';
    }

    return this.isSidebarCollapsed() ? 'menu' : 'menu_open';
  });
  protected readonly activeSection = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(() => this.resolveSectionLabel(this.router.url)),
      startWith(this.resolveSectionLabel(this.router.url))
    ),
    { initialValue: 'Dashboard' }
  );

  protected onSearch(): void {
    const query = this.searchControl.value.trim();

    this.router.navigate(['/providers'], {
      queryParams: query ? { q: query } : {}
    });
  }

  protected toggleSidebar(): void {
    if (this.isHandset()) {
      this.mobileSidebarOpen.update((isOpen) => !isOpen);
      return;
    }

    this.desktopSidebarCollapsed.update((isCollapsed) => !isCollapsed);
  }

  protected closeMobileSidebar(): void {
    if (this.isHandset()) {
      this.mobileSidebarOpen.set(false);
    }
  }

  protected logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private resolveSectionLabel(url: string): string {
    const path = url.split('?')[0];
    const sortedRoutes = [...this.navItems].sort((a, b) => b.route.length - a.route.length);

    return (
      sortedRoutes.find((item) => path === item.route || path.startsWith(`${item.route}/`))?.label ??
      'Dashboard'
    );
  }
}
