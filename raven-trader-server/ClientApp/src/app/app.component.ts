import { Component, OnInit, ElementRef, ViewChild } from '@angular/core';
import { DataService, DTOParseSignedPartial, SwapType, DTOAssetListing } from './services/data.service';
import { Router } from '@angular/router';

declare const $: any; //jQuery hack

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  SwapType: typeof SwapType = SwapType;

  title = 'app';

  @ViewChild("swapHex", { static: true }) public swapHex: ElementRef;

  public previewModeTypeRemap: any = {
    0: "Sale", //SwapType.Buy
    1: "Purchase", //SwapType.Sell
    2: "Trade" //SwapType.Trade
  };

  public signedPartialEntry = "";
  public signedPartialData: DTOParseSignedPartial = null;

  public searchText: string;
  public lastSearch: string;

  public previewMode = false;

  public constructor(public dataService: DataService, private router: Router) {
  }

  ngOnInit(): void {
  }

  omnisearchSubmit(e: Event): void {
    if (this.searchText === this.lastSearch) return;
    this.lastSearch = this.searchText
    console.log("Omni-Search Submitted - " + this.searchText)

    let searchType = "unknown";

    if (/[0-9a-zA-Z]{64}/.test(this.searchText)) {
      //TXID/Blockhash
      searchType = "Hex64";
    } else if (this.searchText.length >= 4 && this.searchText.length < 31) {
      //Asset name (fallback)
      searchType = "Asset"
    }

    switch (searchType) {
      case "Hex64":
        //TODO: Ask server to check this hash for us
        console.log("Womp Womp");
        break;
      case "Asset":
        console.log("Asset");
        this.router.navigate(["/asset", this.searchText])
        break;
      case "unknown":
        console.log("Unknown search type");
        break;
    }
  }

  handleKeyUp(e: KeyboardEvent) {
    //console.log(e.keyCode)
    if (e.keyCode === 13) {
      //this.omnisearchSubmit(e);
    }
  }

  showDetails(listing: DTOAssetListing): void {
    $(this.swapHex.nativeElement).attr("readonly", "")
    this.previewMode = true;
    this.signedPartialEntry = this.base64ToHex(listing.b64SignedPartial);
    this.signedPartialData = {
      result: listing,
      valid: true
    };
    $("#addSwapModal").modal("show");
  }

  delayedCopy(msDelay: number): void {
    setTimeout(() => {
      this.copySwapHex();
    }, msDelay);
  }

  copySwapHex(): void {
    console.log(this.swapHex.nativeElement.value)
    this.swapHex.nativeElement.select();
    document.execCommand('copy');
    this.swapHex.nativeElement.setSelectionRange(0, 0);

    $("#modalSwapFooter").attr("class", "text-success");
    $("#modalSwapFooter").text("Copied!");
    setTimeout(() => {
      $("#modalSwapFooter").text("");
    }, 1500);
  }

  showSubmitWindow(event: MouseEvent): void {
    this.signedPartialEntry = "";
    this.signedPartialData = null;
    this.previewMode = false;
    $(this.swapHex.nativeElement).removeAttr("readonly");
    $("#addSwapModal").modal("show");
  }

  partialChanged(newValue: string): void {
    if (this.previewMode) return;

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

  base64ToHex(str): string {
    const raw = atob(str);
    let result = '';
    for (let i = 0; i < raw.length; i++) {
      const hex = raw.charCodeAt(i).toString(16);
      result += (hex.length === 2 ? hex : '0' + hex);
    }
    return result.toUpperCase();
  }

}
