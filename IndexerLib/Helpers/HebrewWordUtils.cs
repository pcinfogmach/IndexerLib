namespace IndexerLib.Helpers
{
    using System;
    using System.Linq;

    public static class HebrewWordUtils
    {
        // Check if the word is "impossibly long" based on the heuristics
        public static bool IsImpossiblyLongHebrew(string word, int maxLength = 20, int maxRepeatingCount = 4)
        {
            // Check if the word length is greater than maxLength
            if (word.Length > maxLength)
            {
                return true;
            }

            // Check if the word contains repeating characters more than allowed (e.g., ננננננ)
            if (HasRepeatingCharacters(word, maxRepeatingCount))
            {
                return true;
            }

            // Check if the word contains non-Hebrew characters
            if (ContainsNonHebrewCharacters(word))
            {
                return true;
            }

            return false;
        }

        // Heuristic: Check for repeating characters (e.g., "נננננ" should be flagged if more than maxRepeatingCount)
        private static bool HasRepeatingCharacters(string word, int maxRepeatingCount)
        {
            char? lastChar = null;
            int currentRepeatingCount = 0;

            foreach (var c in word)
            {
                if (c == lastChar)
                {
                    currentRepeatingCount++;
                    if (currentRepeatingCount > maxRepeatingCount) // If repetition exceeds allowed count
                        return true;
                }
                else
                {
                    lastChar = c;
                    currentRepeatingCount = 1;
                }
            }

            return false;
        }

        // Heuristic: Check for non-Hebrew characters (anything outside the range of Hebrew letters)
        private static bool ContainsNonHebrewCharacters(string word)
        {
            return word.Any(c => !(c >= 'א' && c <= 'ת'));
        }
    }
}
