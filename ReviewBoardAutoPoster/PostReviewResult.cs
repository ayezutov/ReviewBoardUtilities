namespace ReviewBoardTfsAutoMerger
{
    public class PostReviewResult
    {
        public static PostReviewResult Error
        {
            get { return new PostReviewResult {IsSuccess = false}; }
        }

        public int ReviewId { get; set; }

        public bool IsNewRequest { get; set; }

        public bool IsSuccess { get; set; }

        public bool SkipRevision { get; set; }
    }
}