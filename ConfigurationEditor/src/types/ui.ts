export interface UIState {
    isSidebarOpen: boolean;
    theme: 'light' | 'dark';
    fontSize: number;
}

export interface TableState {
    sortColumn: string;
    sortDirection: 'asc' | 'desc';
    pageSize: number;
    currentPage: number;
}