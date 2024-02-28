//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Collections.Generic;
using System.Globalization;

namespace Azure.AI.Details.Common.CLI
{
    public class WerFraction
    {
        public WerFraction(int words, int errors)
        {
            Words = Math.Max(1, words);
            Errors = errors;
        }

        public int Words { get; set; }
        public int Errors { get; set; }

        public int ErrorRate { get { return Errors * 100 / Words; } }
    }

    public class WerHelpers
    {
        public static WerFraction CalculateWer(string s1, string s2, CultureInfo culture, bool ignorePunctuation = true)
        {
            var trim = ignorePunctuation ? " .?!".ToCharArray() : " ".ToCharArray();
            var words1 = s1.Trim(trim).Split(' ');
            var words2 = s2.Trim(trim).Split(' ');

            var errors = Levenshtein<string>(words1, words2, (a, b) => string.Compare(a, b, false, culture) == 0);
            var words = words1.Length;

            return new WerFraction(words, errors);
        }

        private static int Levenshtein<T>(T[] s1, T[] s2, Func<T, T, bool> compare)
        {
            int n = s1.Length;
            int m = s2.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = compare(s2[j - 1], s1[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}
