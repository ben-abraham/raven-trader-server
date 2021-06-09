import { Component, Input, ChangeDetectorRef, ChangeDetectionStrategy, ViewEncapsulation, SimpleChanges, Output, OnChanges, OnInit, EventEmitter, TemplateRef } from '@angular/core';
import { DTOAssetSearchResults, DTOHistoryResults, DTOListingResults, DTOSearchResults, SwapType } from '../../services/data.service';

@Component({
  selector: 'simple-table',
  templateUrl: './simple-table.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class SimpleTableComponent implements OnInit, OnChanges {
  SwapType: typeof SwapType = SwapType;

  public results = [];

  @Output() onPageChange: EventEmitter<any> = new EventEmitter();

  @Input() public serverResponse: DTOSearchResults<any>;

  @Input() public dataField = "swaps";

  @Input() public headerTemplate: TemplateRef<any>;
  @Input() public bodyTemplate: TemplateRef<any>;

  @Input() public totalCount = 0;
  @Input() public pageSize = 20;
  @Output() public offset = 0;

  constructor(private cd: ChangeDetectorRef) {
  }

  ngOnInit() {
  }

  ngOnChanges(simpleChange: SimpleChanges) {
    if (simpleChange.serverResponse) {
      this.updateTableData();
    }
  }

  updateTableData() {
    if (!this.serverResponse) {
      this.results = [];
      this.totalCount = 0;
      this.offset = 0;
      return;
    }

    
    this.results = this.serverResponse[this.dataField];
    this.totalCount = this.serverResponse.totalCount;
    this.offset = this.serverResponse.offset;
  }

  pageChanged(event: any) {
    this.onPageChange.emit(event);
  }
}
