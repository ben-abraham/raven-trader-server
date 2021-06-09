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
  public offset = 0;
  public pageSize = 25;

  public constructor(public app: AppComponent,
    public dataService: DataService) { }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.dataService.queryListings("", this.offset, this.pageSize).subscribe(results => {
      this.results = results;
    });
  }

  pageChanged(event: any) {
    this.offset = event.first;
    this.loadData();
  }

  showDetails(order: MouseEvent, listing: DTOAssetListing): void {
    this.app.showDetails(listing);
  }

}
