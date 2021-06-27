import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { SwapListComponent } from './swaps/swaps.component';

import { AssetListComponent } from './assets/asset-list.component';
import { AssetDetailsComponent } from './asset-details/asset-details.component';

import { SimpleTableComponent } from './components/simple-table/simple-table.component';
import { PaginatorComponent } from './components/paginator/paginator.component';

import { DocumentationComponent } from './documentation/documentation.component';


import { DataService } from './services/data.service';
import { SafeUrlPipe } from './services/safe-url.pipe';


@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    DashboardComponent,
    SwapListComponent,
    AssetDetailsComponent,
    SimpleTableComponent,
    PaginatorComponent,
    AssetListComponent,
    DocumentationComponent,
    SafeUrlPipe
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    CommonModule,
    RouterModule.forRoot([
      { path: '', component: DashboardComponent, pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'swaps', component: SwapListComponent },
      { path: 'assets', component: AssetListComponent },
      { path: 'asset/:name', component: AssetDetailsComponent },
      { path: 'help', component: DocumentationComponent },
    ])
  ],
  providers: [DataService],
  bootstrap: [AppComponent]
})
export class AppModule { }
