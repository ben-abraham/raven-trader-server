import { Injectable } from '@angular/core';
import { Subject, Observable, ReplaySubject } from 'rxjs';

import { HttpClient, HttpParams, HttpHeaders } from '@angular/common/http';

export enum SwapType {
  Buy = 0,
  Sell = 1,
  Trade = 2
}

@Injectable()
export class DataService {
  private siteDataMsg: ReplaySubject<DTOSiteData>;

  constructor(private http: HttpClient)
  {
    this.siteDataMsg = new ReplaySubject<DTOSiteData>(1);

    this.updateSiteDate();
  }

  public updateSiteDate(): void {
    this.http.get<DTOSiteData>('api/sitedata').subscribe((newData) => {
      this.siteDataUpdated(newData);
    });
  }

  public querySite(): void {

  }

  public queryListings(assetName: string = null, offset=0, pageSize=25, swapType: SwapType = null): Observable<DTOListingResults> {
    let params = new HttpParams();
    params = params.append("assetName", assetName);
    params = params.append("offset", offset.toString());
    params = params.append("pageSize", pageSize.toString());
    params = params.append("swapType", swapType ? SwapType[swapType] : null);
    return this.http.get<DTOListingResults>('api/sitedata/listings', { params: params });
  }

  public queryHistory(assetName: string = null, offset = 0, pageSize = 25, swapType: SwapType = null): Observable<DTOHistoryResults> {
    let params = new HttpParams();
    params = params.append("assetName", assetName);
    params = params.append("offset", offset.toString());
    params = params.append("pageSize", pageSize.toString());
    params = params.append("swapType", swapType ? SwapType[swapType] : null);
    return this.http.get<DTOHistoryResults>('api/sitedata/swaphistory', { params: params });
  }

  public queryAsset(assetName: string): Observable<DTOAssetSearchResults> {
    let headers = new HttpHeaders();
    headers = headers.append('Content-Type', 'application/json');
    let params = new HttpParams();
    params = params.append("assetName", assetName);
    return this.http.get<DTOAssetSearchResults>('api/sitedata/asset', { headers: headers, params: params });
  }

  //Stringify only works for post
  public parseSignedPartial(signedPartialHex: string): Observable<DTOParseSignedPartial> {
    const headers = {
      "Content-Type": 'application/json',
      "Accept": 'application/json'
    };
    return this.http.post<DTOParseSignedPartial>(
      'api/assets/quickparse',
      JSON.stringify({ hex: signedPartialHex }),
      { 'headers': headers }
    );
  }

  public listSignedPartial(signedPartialHex: string): Observable<DTOParseSignedPartial> {
    const headers = {
      "Content-Type": 'application/json',
      "Accept": 'application/json'
    };
    return this.http.post<DTOParseSignedPartial>(
      'api/assets/list',
      JSON.stringify({ hex: signedPartialHex }),
      { 'headers': headers }
    );
  }


  public getData(): Observable<DTOSiteData> {
    return this.siteDataMsg.asObservable();
  }
  public siteDataUpdated(siteData: DTOSiteData): void {
    this.siteDataMsg.next(siteData);
  }
}

export interface DTOAssetVolume {
  total: number;
  swap: number;
};

export interface AssetLookup<T> {
  [AssetName: string]: T;
};

export interface SwapDetails {
  orderType: SwapType;
  inType: string;
  inQuantity: number;
  outType: string;
  outQuantity: number;
  unitPrice: number;
}

export interface DTOSwap extends SwapDetails {
  txid: string;
  block: number;
};

export interface DTOSiteData {
  block: number;
  hash: string;
  recent_swaps: DTOSwap[];
  asset_volume: AssetLookup<DTOAssetVolume>;
};

export interface DTOAssetSearchResults {
  asset: string;
  children: string[];
  units: number;
  denomination: number;
  amount: number;
  ipfs: string;
  reissuable: boolean;
  buyOrders: number;
  buyQuantity: number;
  buys: DTOAssetListing[];
  sellOrders: number;
  sellQuantity: number;
  sells: DTOAssetListing[];
  tradeOrders: number;
  tradeQuantity: number;
  trades: DTOAssetListing[];
}

export interface DTOAssetListing extends SwapDetails {
  utxo: string;
  b64SignedPartial: string;
}

export interface DTOParseSignedPartial {
  valid: boolean;
  result: DTOAssetListing;
}


export interface DTOSearchResults<T> {
  swaps: T[];
  offset: number;
  totalCount: number;
}

export interface DTOListingResults extends DTOSearchResults<DTOAssetListing> {
}

export interface DTOHistoryResults extends DTOSearchResults<DTOSwap> {
}


