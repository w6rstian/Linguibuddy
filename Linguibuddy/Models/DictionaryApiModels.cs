using Newtonsoft.Json;

namespace Linguibuddy.Models
{
    public class Definition
    {
        [JsonProperty("definition")]
        public string DefinitionText { get; set; } = string.Empty;

        [JsonProperty("example")]
        public string Example { get; set; } = string.Empty;

        [JsonProperty("synonyms")]
        public List<string> Synonyms { get; set; } = [];

        [JsonProperty("antonyms")]
        public List<string> Antonyms { get; set; } = [];
    }

    public class Meaning
    {
        [JsonProperty("partOfSpeech")]
        public string PartOfSpeech { get; set; } = string.Empty;

        [JsonProperty("definitions")]
        public List<Definition> Definitions { get; set; } = [];
    }

    public class Phonetic
    {
        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        [JsonProperty("audio")]
        public string Audio { get; set; } = string.Empty;
    }

    public class WordEntry
    {
        [JsonProperty("word")]
        public string Word { get; set; } = string.Empty;

        [JsonProperty("phonetic")]
        public string Phonetic { get; set; } = string.Empty;

        [JsonProperty("phonetics")]
        public List<Phonetic> Phonetics { get; set; } = [];

        [JsonProperty("origin")]
        public string Origin { get; set; } = string.Empty;

        [JsonProperty("meanings")]
        public List<Meaning> Meanings { get; set; } = [];
    }
}