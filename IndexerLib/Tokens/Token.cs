using IndexerLib.Tokens;
using System.Collections.Generic;

namespace IndexerLib
{
    public class Token
    {
        public int ID { get; set; }
        public List<Postings> Postings { get; set; } = new List<Postings>();
    }

   
    public class Postings
    {
        public int Position { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }
    }
}
