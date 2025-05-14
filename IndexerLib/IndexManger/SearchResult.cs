using System.Collections.Generic;

namespace IndexerLib.IndexManger
{
    public class SearchResult
    {
        public int FileId { get; set; }
        public List<Postings[]> MatchedPostings { get; set; } = new List<Postings[]>();
    }
}
