import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { DataService, DTOSiteData, DTOSwap, DTOListingResults, DTOAssetListing, SwapType } from '../services/data.service';
import { Subscribable, Subject } from 'rxjs';
import { AppComponent } from '../app.component';

@Component({
  selector: 'swaps-component',
  templateUrl: './swaps.component.html'
})
export class SwapListComponent implements OnInit {
  SwapType: typeof SwapType = SwapType;

  public results: DTOListingResults;

  public constructor(public app: AppComponent,
    public dataService: DataService) { }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.dataService.queryListings("").subscribe(results => {
      this.results = results;
    });
  }

  showDetails(order: MouseEvent, listing: DTOAssetListing): void {
    this.app.showDetails(listing);
  }

}
