import { Component, OnInit } from '@angular/core';
import { DataService, DTOSiteData, DTOSwap, DTOListingResults } from '../services/data.service';
import { Subscribable, Subject } from 'rxjs';

@Component({
  selector: 'swaps-component',
  templateUrl: './swaps.component.html'
})
export class SwapListComponent implements OnInit {

  public results: DTOListingResults;

  public constructor(public dataService: DataService) { }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.dataService.queryListings("RVNTEST1").subscribe(results => {
      console.log("Got Swaps: ", results);
      this.results = results;
    });
  }

}
