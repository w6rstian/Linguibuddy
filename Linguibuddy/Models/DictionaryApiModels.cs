using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Linguibuddy.Models
{
    public class DictionaryWord
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }

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

        // moje pola
        public string Audio { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;
    }

    public class Phonetic
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }

        [JsonIgnore]
        public int DictionaryWordId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        [JsonProperty("audio")]
        public string Audio { get; set; } = string.Empty;
    }

    public class Meaning
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }

        [JsonIgnore]
        public int DictionaryWordId { get; set; }

        [JsonProperty("partOfSpeech")]
        public string PartOfSpeech { get; set; } = string.Empty;

        [JsonProperty("definitions")]
        public List<Definition> Definitions { get; set; } = [];
    }

    public class Definition
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }

        [JsonIgnore]
        public int MeaningId { get; set; }

        [JsonProperty("definition")]
        public string DefinitionText { get; set; } = string.Empty;

        [JsonProperty("example")]
        public string Example { get; set; } = string.Empty;

        [JsonProperty("synonyms")]
        public List<string> Synonyms { get; set; } = [];

        [JsonProperty("antonyms")]
        public List<string> Antonyms { get; set; } = [];

        // tłumaczenie z API
        public string? Translation { get; set; }
    }
}