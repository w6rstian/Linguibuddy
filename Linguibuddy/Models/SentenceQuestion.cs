using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Models
{
    public class SentenceQuestion
    {
        public string EnglishSentence { get; set; } = string.Empty;
        public string PolishTranslation { get; set; } = string.Empty;

        // Opcjonalnie: Poziom trudności, kategoria itp.
    }
}
