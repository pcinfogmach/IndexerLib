namespace IndexerLib.Index
{
    public class IndexKey
    {
        public byte[] Hash { get; set; }
        public long Offset { get; set; }
        public int Length { get; set; }
    }
}
