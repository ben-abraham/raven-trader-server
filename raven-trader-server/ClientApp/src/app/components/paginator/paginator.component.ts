/// <reference path="../simple-table/simple-table.component.ts" />
import { OnInit, OnChanges, Input, Output, TemplateRef, ChangeDetectorRef, SimpleChanges, EventEmitter, Component, ViewEncapsulation, ChangeDetectionStrategy } from "@angular/core";



@Component({
  selector: 'paginator',
  templateUrl: './paginator.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None
})
export class PaginatorComponent implements OnInit, OnChanges {

  @Input() pageLinkSize: number = 5;
  @Output() onPageChange: EventEmitter<any> = new EventEmitter();
  @Input() style: any;
  @Input() styleClass: string;
  @Input() alwaysShow: boolean = true;
  @Input() templateLeft: TemplateRef<any>;
  @Input() templateRight: TemplateRef<any>;
  @Input() dropdownAppendTo: any;
  @Input() dropdownScrollHeight: string = '200px';
  @Input() currentPageReportTemplate: string = '{first} - {last} / {totalRecords}';
  @Input() showCurrentPageReport: boolean = true;
  @Input() showFirstLastIcon: boolean = true;
  @Input() totalRecords: number = 0;
  @Input() rows: number = 0;
  @Input() rowsPerPageOptions: any[];
  @Input() showJumpToPageDropdown: boolean;
  @Input() showPageLinks: boolean = true;

  @Input() dropdownItemTemplate: TemplateRef<any>;

  pageLinks: number[];

  pageItems: any[];

  rowsPerPageItems: any[];

  paginatorState: any;

  _first: number = 0;

  _page: number = 0;

  constructor(private cd: ChangeDetectorRef) { }

  ngOnInit() {
    this.updatePaginatorState();
  }

  ngOnChanges(simpleChange: SimpleChanges) {
    if (simpleChange.totalRecords) {
      this.updatePageLinks();
      this.updatePaginatorState();
      this.updateFirst();
      this.updateRowsPerPageOptions();
    }

    if (simpleChange.first) {
      this._first = simpleChange.first.currentValue;
      this.updatePageLinks();
      this.updatePaginatorState();
    }

    if (simpleChange.rows) {
      this.updatePageLinks();
      this.updatePaginatorState();
    }

    if (simpleChange.rowsPerPageOptions) {
      this.updateRowsPerPageOptions();
    }
  }

  @Input() get first(): number {
    return this._first;
  }
  set first(val: number) {
    this._first = val;
  }

  updateRowsPerPageOptions() {
    if (this.rowsPerPageOptions) {
      this.rowsPerPageItems = [];
      for (let opt of this.rowsPerPageOptions) {
        if (typeof opt == 'object' && opt['showAll']) {
          this.rowsPerPageItems.unshift({ label: opt['showAll'], value: this.totalRecords });
        }
        else {
          this.rowsPerPageItems.push({ label: String(opt), value: opt });
        }
      }
    }
  }

  isFirstPage() {
    return this.getPage() === 0;
  }

  isLastPage() {
    return this.getPage() === this.getPageCount() - 1;
  }

  getPageCount() {
    return Math.ceil(this.totalRecords / this.rows) || 1;
  }

  calculatePageLinkBoundaries() {
    let numberOfPages = this.getPageCount(),
      visiblePages = Math.min(this.pageLinkSize, numberOfPages);

    //calculate range, keep current in middle if necessary
    let start = Math.max(0, Math.ceil(this.getPage() - ((visiblePages) / 2))),
      end = Math.min(numberOfPages - 1, start + visiblePages - 1);

    //check when approaching to last page
    var delta = this.pageLinkSize - (end - start + 1);
    start = Math.max(0, start - delta);

    return [start, end];
  }

  updatePageLinks() {
    this.pageLinks = [];
    let boundaries = this.calculatePageLinkBoundaries(),
      start = boundaries[0],
      end = boundaries[1];

    for (let i = start; i <= end; i++) {
      this.pageLinks.push(i + 1);
    }

    if (this.showJumpToPageDropdown) {
      this.pageItems = [];
      for (let i = 0; i < this.getPageCount(); i++) {
        this.pageItems.push({ label: String(i + 1), value: i });
      }
    }
  }

  changePage(p: number) {
    var pc = this.getPageCount();

    if (p >= 0 && p < pc) {
      this._first = this.rows * p;
      var state = {
        page: p,
        first: this.first,
        rows: this.rows,
        pageCount: pc
      };
      this.updatePageLinks();

      this.onPageChange.emit(state);
      this.updatePaginatorState();
    }
  }

  updateFirst() {
    const page = this.getPage();
    if (page > 0 && this.totalRecords && (this.first >= this.totalRecords)) {
      Promise.resolve(null).then(() => this.changePage(page - 1));
    }
  }

  getPage(): number {
    return Math.floor(this.first / this.rows);
  }

  changePageToFirst(event) {
    if (!this.isFirstPage()) {
      this.changePage(0);
    }

    event.preventDefault();
  }

  changePageToPrev(event) {
    this.changePage(this.getPage() - 1);
    event.preventDefault();
  }

  changePageToNext(event) {
    this.changePage(this.getPage() + 1);
    event.preventDefault();
  }

  changePageToLast(event) {
    if (!this.isLastPage()) {
      this.changePage(this.getPageCount() - 1);
    }

    event.preventDefault();
  }

  onPageLinkClick(event, page) {
    this.changePage(page);
    event.preventDefault();
  }

  onRppChange(event) {
    this.changePage(this.getPage());
  }

  onPageDropdownChange(event) {
    this.changePage(event.value);
  }

  updatePaginatorState() {
    this.paginatorState = {
      page: this.getPage(),
      pageCount: this.getPageCount(),
      rows: this.rows,
      first: this.first,
      totalRecords: this.totalRecords
    }
  }

  get currentPageReport() {
    return this.currentPageReportTemplate
      .replace("{currentPage}", String(this.getPage() + 1))
      .replace("{totalPages}", String(this.getPageCount()))
      .replace("{first}", String((this.totalRecords > 0) ? this._first + 1 : 0))
      .replace("{last}", String(Math.min(this._first + this.rows, this.totalRecords)))
      .replace("{rows}", String(this.rows))
      .replace("{totalRecords}", String(this.totalRecords));
  }
}
