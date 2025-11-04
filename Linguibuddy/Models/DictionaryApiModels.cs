namespace Linguibuddy.Models
{
    public class Entry
    {
        public Language Language { get; set; } = new();
        public string PartOfSpeech { get; set; } = string.Empty;
        public List<Pronunciation> Pronunciations { get; set; } = [];
        public List<Form> Forms { get; set; } = [];
        public List<Sense> Senses { get; set; } = [];
        public List<string> Synonyms { get; set; } = [];
        public List<string> Antonyms { get; set; } = [];
    }

    public class Form
    {
        public string Word { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = [];
    }

    public class Language
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class License
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class Pronunciation
    {
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = [];
    }

    public class Quote
    {
        public string Text { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }

    public class DictionaryResponse
    {
        public string Word { get; set; } = string.Empty;
        public List<Entry> Entries { get; set; } = [];
        public Source Source { get; set; } = new();
    }

    public class Sense
    {
        public string Definition { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = [];
        public List<string> Examples { get; set; } = [];
        public List<Quote> Quotes { get; set; } = [];
        public List<string> Synonyms { get; set; }  = [];
        public List<string> Antonyms { get; set; } = [];
        public List<Translation> Translations { get; set; } = [];
        public List<Sense> Subsenses { get; set; } = [];
    }

    public class Source
    {
        public string Url { get; set; } = string.Empty;
        public License License { get; set; } = new();
    }

    public class Translation
    {
        public Language Language { get; set; } = new();
        public string Word { get; set; } = string.Empty;
    }
}