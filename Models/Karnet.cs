using System;

namespace FISTNESS.Models
{
    public class Karnet
    {
        public int Id { get; set; }
        public string TypKarnetu { get; set; } // "student", "normalny", "brak", itp.
        public string UserId { get; set; }
        public DateTime? DataWaznosci { get; set; }

    }
}


