using System;
using System.Collections.Generic;

namespace Backend.Common.Pagination
{
    public class PageResponse<T>
    {
        public IEnumerable<T> Content { get; set; } = new List<T>();
        public int Page { get; set; }
        public int Size { get; set; }
        public long TotalElements { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalElements / (Size > 0 ? Size : 10));
        public bool HasNext => Page + 1 < TotalPages;
        public bool HasPrevious => Page > 0;
    }
}
