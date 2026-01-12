namespace Linguibuddy.Models
{
    public static class SuperMemoGrade
    {
        /// <summary>"Total blackout", complete failure to recall the information.</summary>
        public const int Null = 0;

        /// <summary>Incorrect response, but upon seeing the correct answer it felt familiar.</summary>
        public const int Bad = 1;

        /// <summary>Incorrect response, but upon seeing the correct answer it seemed easy to remember.</summary>
        public const int Fail = 2;

        /// <summary>Correct response, but required significant effort to recall.</summary>
        public const int Pass = 3;

        /// <summary>Correct response, after some hesitation.</summary>
        public const int Good = 4;

        /// <summary>Correct response with perfect recall.</summary>
        public const int Bright = 5;

        // Próg zaliczenia (wszystko poniżej resetuje interwał do 1 dnia)
        public const int PassingThreshold = 3;
    }
}
