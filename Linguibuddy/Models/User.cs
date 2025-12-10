using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Models
{
    public class User
    {
        public int Id { get; set; }

        // [MaxLength(wartość)] - tylko do odczytu w bazie danych, tylko maksymalna długość znaków
        // [StringLength(wartość, ErrorMessage = "Komunikat o błędzie] - do odczytu i zapisu w bazie danych, maksymalna długość znaków z komunikatem o błędzie, możliwość dania minimalnej długości
        // {0} = Property Name, {1} = Max Length, {2} = Min Length
        [StringLength(50, ErrorMessage = "{0} can have a max of {1} characters")]
        public required string UserName { get; set; }
    }
}
