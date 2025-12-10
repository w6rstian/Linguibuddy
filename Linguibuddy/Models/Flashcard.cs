using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Models
{
    public class Flashcard
    {
        public int Id { get; set; }
        public string Word { get; set; }
        public string Translation { get; set; }
        public string PartOfSpeech { get; set; }
        public string ExampleSentence { get; set; }
    }
}