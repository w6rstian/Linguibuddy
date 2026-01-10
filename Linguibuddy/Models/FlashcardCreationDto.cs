namespace Linguibuddy.Models
{
    public class FlashcardCreationDto
    {
        public string Word { get; set; } = string.Empty;
        public string Phonetic { get; set; } = string.Empty;
        public string Audio { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;


        public string PartOfSpeech { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;

        public string Translation { get; set; } = string.Empty;
    }
}
