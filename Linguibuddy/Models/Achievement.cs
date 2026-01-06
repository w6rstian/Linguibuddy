using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Models
{
    public class Achievement
    {
        public int Id;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public string IconUrl = string.Empty;
        public string Crieria = string.Empty; // Jakieś warunki odblokowania w Json
    }
}
