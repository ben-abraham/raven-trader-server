import { Component, OnInit } from '@angular/core';
import { DataService, DTOSiteData, DTOSwap, SwapType } from '../services/data.service';
import { Subscribable, Subject } from 'rxjs';

@Component({
  selector: 'dashboard-component',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  SwapType: typeof SwapType = SwapType;

  public dataSub: Subscribable<DTOSiteData>;
  public data: DTOSiteData;

  public showAdmin = false;
  public assetVol: VolumeRow[];
  public swaps: DTOSwap[];

  public constructor(public dataService: DataService) { }

  ngOnInit(): void
  {
    this.dataSub = this.dataService.getData();
    this.dataSub.subscribe((newData) => this.dataUpdated(newData));
  }

  ngOnDestroy(): void {
  }

  public dataUpdated(siteData: DTOSiteData) {
    if (siteData) { 
      this.data = siteData;
      this.assetVol = [];
      this.swaps = this.data.recent_swaps;
      for (const aName in this.data.asset_volume) {
        this.assetVol.push({
          AssetName: aName,
          TotalVolume: siteData.asset_volume[aName].total,
          SwapVolume: siteData.asset_volume[aName].swap
        }); 
      }
    }
  }

}

interface VolumeRow {
  AssetName: string;
  TotalVolume: number;
  SwapVolume: number;
}
