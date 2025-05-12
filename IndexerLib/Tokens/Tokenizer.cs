using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndexerLib.Tokens
{
    public static class Tokenizer
    {
        public static Dictionary<string, Token> Tokenize(string text, string path)
        {
            text = text.ToLower().Replace("nbsp", "####");

            bool inWord = false;
            bool doubleQuotesDetected = false;

            var tokens = new Dictionary<string, Token>();
            int position = 1;
            int currentIndex = -1;

            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                currentIndex = i;
                char c = text[i];

                if (c == '־') 
                    c = ' ';

                // Skip HTML tags: anything between < and >
                if (c == '<')
                {
                    int tagEnd = text.IndexOf('>', i);
                    if (tagEnd != -1)
                    {
                        i = tagEnd; // Skip to character after '>'
                        continue;
                    }
                }

                if (IsHebrewLetterOrDiacritic(c))
                {
                    if (doubleQuotesDetected)
                    {
                        stringBuilder.Append('"');
                        doubleQuotesDetected = false;
                    }
                    stringBuilder.Append(c);
                    inWord = true;
                }
                else if (inWord && c == '\'')
                {
                    stringBuilder.Append(c);
                }
                else if (inWord && c == '"')
                {
                    doubleQuotesDetected = true;
                }
                else
                {
                    if (stringBuilder.Length > 0)
                        AddWord();

                    doubleQuotesDetected = false;
                    inWord = false;
                }
            }

            // Handle last word if string ends with it
            if (stringBuilder.Length > 0)
            {
                AddWord();
            }

            void AddWord()
            {
                string word = stringBuilder.ToString();
                string cleanedWord = word.RemoveHebrewDiactrics();

                stringBuilder.Clear();

                if (!tokens.ContainsKey(cleanedWord))
                    tokens[cleanedWord] = new Token { FilePath = path };
                tokens[cleanedWord].Postings.Add(new Postings
                {
                    Length = word.Length,
                    Position = position++,
                    StartIndex = currentIndex - word.Length
                });
            }

            return tokens;
        }

        private static bool IsHebrewLetterOrDiacritic(char c)
        {
            return char.IsLetter(c) || (c >= '\u0591' && c <= '\u05C7');
        }


        public static string RemoveHebrewDiactrics(this string input)
        {
            var sb = new StringBuilder(input.Length);

            foreach (var c in input)
                if (c > 1487 || c < 1425)
                    sb.Append(c);

            return sb.ToString();
        }

    }
}
