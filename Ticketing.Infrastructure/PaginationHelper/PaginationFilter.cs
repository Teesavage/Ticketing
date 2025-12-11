namespace Ticketing.Infrastructure.PaginationHelper
{
    public class PaginationFilter
    {
        const int maxPageSize = 10000;
        public int PageNumber { get; set; } = 1;
        private int _pageSize { get; set; } = 10;
        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                _pageSize = value > maxPageSize ? maxPageSize : value;
            }
        }

        // Search and filter properties
        public string Search { get; set; } = string.Empty;

        }

        public class Meta
        {
            public int TotalPages { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public int PageNumber { get; set; }
            public int NextPage { get; set; }
            public int PreviousPage { get; set; }
            public int FirstPage { get; set; }
            public int LastPage { get; set; }
            public object DataReport { get; set; } // Optional: for summaries, etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public Meta(int totalCount, int pageNumber, int pageSize)
        {
                TotalCount = totalCount;
                PageSize = pageSize;
                PageNumber = pageNumber;
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                FirstPage = 1;
                LastPage = TotalPages;
                NextPage = pageNumber < TotalPages ? pageNumber + 1 : TotalPages;
                PreviousPage = pageNumber > 1 ? pageNumber - 1 : 1;
            }
        }

        public class PageResponse<T>
        {
            public Meta Meta { get; set; }
            public IEnumerable<T> Data { get; set; }

            public PageResponse(IEnumerable<T> data, Meta meta)
            {
                Data = data;
                Meta = meta;
            }
        }
}
