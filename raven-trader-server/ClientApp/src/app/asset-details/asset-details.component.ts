import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { DataService, DTOSiteData, DTOSwap, SwapType, DTOAssetSearchResults, DTOAssetListing } from '../services/data.service';
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
  public parentName: string;

  public assetDetails: DTOAssetSearchResults;
  public assetHistory: DTOSwap[];

  public constructor(public dataService: DataService,
    private app: AppComponent,
    private router: ActivatedRoute,
    private cdr: ChangeDetectorRef) { }

  ngOnInit(): void
  {
    this.router.paramMap.subscribe(params => {
      this.assetName = params.get("name");
      if (this.assetName.includes("/"))
        this.parentName = this.assetName.split("/").slice(0, -1).join("/")
      else
        this.parentName = null;
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
    this.dataService.queryHistory(this.assetName).subscribe(data => {
      this.assetHistory = data.swaps;
      this.cdr.detectChanges();
    });
  }

  showDetails(order: MouseEvent, listing: DTOAssetListing): void {
    this.app.showDetails(listing);
  }

}

