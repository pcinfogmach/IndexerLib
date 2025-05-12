using System.Collections.Generic;

namespace IndexerLib.IndexManger
{
    public class SearchResult
    {
        public string FilePath { get; set; }
        public List<Postings[]> MatchedPostings { get; set; } = new List<Postings[]>();
    }
}
