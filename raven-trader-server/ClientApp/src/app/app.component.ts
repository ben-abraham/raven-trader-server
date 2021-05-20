import { Component, OnInit, ElementRef } from '@angular/core';
import { DataService, DTOParseSignedPartial, SwapType } from './services/data.service';

declare const $: any; //jQuery hack

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  SwapType: typeof SwapType = SwapType;

  title = 'app';

  public signedPartialEntry = "";
  public signedPartialData: DTOParseSignedPartial = null;

  public constructor(public dataService: DataService) {
  }

  ngOnInit(): void {
    ($(".toast") as any).toast();
  }

  partialChanged(newValue: string): void {
    const newHex = this.signedPartialEntry = newValue;
    $("#modalSwapFooter").text("");
    if (/^[0-9a-fA-F]{32,}$/.exec(newHex)) {
      this.dataService.parseSignedPartial(newHex).subscribe((response) => {
        this.signedPartialData = response;
      });
    } else {
      this.signedPartialData = null;
    }
  }

  submitPartial(): void {
    if (this.signedPartialData && this.signedPartialData.valid) {
      console.log("Listing");
      this.signedPartialData.result.orderType
      this.dataService.listSignedPartial(this.signedPartialEntry).subscribe((response) => {
        if (response.valid) {
          console.log("Listed!");
          $("#modalSwapFooter").attr("class", "text-success");
          $("#modalSwapFooter").text("Listed!");
          setTimeout(() => {
            $("#addSwapModal").modal("hide");
            $("#modalSwapFooter").text("");
            this.partialChanged("");
          }, 750);
        } else {
          $("#modalSwapFooter").attr("class", "text-success");
          $("#modalSwapFooter").text("Error, Invalid!");
        }
      });
    }
  }

}
