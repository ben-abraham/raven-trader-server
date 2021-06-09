import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { DataService, DTOSiteData, DTOSwap, DTOListingResults, DTOAssetListing, SwapType, DTOGroupedResults } from '../services/data.service';
import { Subscribable, Subject } from 'rxjs';
import { AppComponent } from '../app.component';

@Component({
  selector: 'asset-list-component',
  templateUrl: './asset-list.component.html'
})
export class AssetListComponent implements OnInit {
  SwapType: typeof SwapType = SwapType;

  public results: DTOGroupedResults;
  public offset = 0;
  public pageSize = 25;

  public constructor(public app: AppComponent,
    public dataService: DataService) { }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.dataService.queryGrouped("", this.offset, this.pageSize).subscribe(results => {
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
