import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { DataService, DTOSiteData, DTOSwap, SwapType, DTOAssetSearchResults, DTOAssetListing, DTOHistoryResults } from '../services/data.service';
import { Subscribable, Subject } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { AppComponent } from '../app.component';

@Component({
  selector: 'asset-details-component',
  templateUrl: './asset-details.component.html'
})
export class AssetDetailsComponent implements OnInit {
  SwapType: typeof SwapType = SwapType;

  public assetName: string;

  public assetDetails: DTOAssetSearchResults;
  public assetHistory: DTOHistoryResults;


  public historyOffset = 0;
  public historyPageSize = 25;

  public constructor(public dataService: DataService,
    private app: AppComponent,
    private router: ActivatedRoute,
    private cdr: ChangeDetectorRef) { }

  ngOnInit(): void
  {
    this.router.paramMap.subscribe(params => {
      this.assetName = params.get("name");
      this.refresh();
    });
  }

  ngOnDestroy(): void {
  }

  public refresh() {
    this.dataService.queryAsset(this.assetName).subscribe(data => {
      this.assetDetails = data;
      this.cdr.detectChanges();
    });
    this.changeHistoryPage();
  }

  public changeHistoryPage() {
    this.dataService.queryHistory(this.assetName, this.historyOffset, this.historyPageSize).subscribe(data => {
      this.assetHistory = data;
      this.cdr.detectChanges();
    });
  }

  showDetails(order: MouseEvent, listing: DTOAssetListing): void {
    this.app.showDetails(listing);
  }

  historyPageChanged(event: any) {
    this.historyOffset = event.first;
    this.changeHistoryPage();
  }

}

