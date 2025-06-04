namespace CsvProcessing;

public static class TokenSetSimilarity
{
    public static int TokenSetRatio(string s1, string s2)
    {
        if (string.IsNullOrWhiteSpace(s1) || string.IsNullOrWhiteSpace(s2))
            return 0;

        var tokens1 = Tokenize(s1);
        var tokens2 = Tokenize(s2);

        var set1 = new HashSet<string>(tokens1);
        var set2 = new HashSet<string>(tokens2);

        var intersection = new HashSet<string>(set1);
        intersection.IntersectWith(set2);

        set1.ExceptWith(intersection);
        set2.ExceptWith(intersection);

        var sortedInter = JoinSorted(intersection);
        var sorted1 = JoinSorted(intersection.Concat(set1));
        var sorted2 = JoinSorted(intersection.Concat(set2));

        int ratio1 = Ratio(sortedInter, sorted1);
        int ratio2 = Ratio(sortedInter, sorted2);
        int ratio3 = Ratio(sorted1, sorted2);

        return Math.Max(ratio3, Math.Max(ratio1, ratio2));
    }

    private static List<string> Tokenize(string input)
    {
        var span = input.AsSpan();
        int len = span.Length;
        int wordStart = -1;
        var tokens = new List<string>();

        for (int i = 0; i <= len; i++)
        {
            var isEnd = i == len || char.IsWhiteSpace(span[i]);
            if (!isEnd && wordStart == -1)
            {
                wordStart = i;
            }
            else if (isEnd && wordStart != -1)
            {
                tokens.Add(span[wordStart..i].ToString());
                wordStart = -1;
            }
        }

        return tokens;
    }


    private static string JoinSorted(IEnumerable<string> tokens)
    {
        const int MaxInitial = 32;
        string[] buffer = new string[MaxInitial];
        int count = 0;

        foreach (var token in tokens)
        {
            if (count >= buffer.Length)
                Array.Resize(ref buffer, buffer.Length * 4);
            buffer[count++] = token;
        }

        if (count == 0) return string.Empty;

        Array.Sort(buffer, 0, count, StringComparer.Ordinal);

        int totalLen = count - 1;
        for (int i = 0; i < count; i++) totalLen += buffer[i].Length;

        var sb = new System.Text.StringBuilder(totalLen);
        sb.Append(buffer[0]);
        for (int i = 1; i < count; i++)
        {
            sb.Append(' ');
            sb.Append(buffer[i]);
        }

        return sb.ToString();
    }

    public static int Ratio(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 100;
        if (a.Equals(b, StringComparison.Ordinal)) return 100;

        int distance = LevenshteinUnsafe(a, b);
        int maxLen = Math.Max(a.Length, b.Length);
        return (int)((1.0 - (double)distance / maxLen) * 100);
    }

    public static int Levenshtein(string s, string t)
    {
        int lenS = s.Length;
        int lenT = t.Length;

        if (lenS == 0) return lenT;
        if (lenT == 0) return lenS;

        int[] row0 = new int[lenT + 1];
        int[] row1 = new int[lenT + 1];

        for (int j = 0; j <= lenT; j++)
            row0[j] = j;

        for (int i = 1; i <= lenS; i++)
        {
            row1[0] = i;
            for (int j = 1; j <= lenT; j++)
            {
                int cost = (s[i - 1] ^ t[j - 1]) == 0 ? 0 : 1;
                row1[j] = row1[j - 1] + 1;
                if (row0[j] + 1 < row1[j]) row1[j] = row0[j] + 1;
                int replace = row0[j - 1] + cost;
                if (replace < row1[j]) row1[j] = replace;            }

            (row0, row1) = (row1, row0);
        }

        return row0[lenT];
    }
    public static unsafe int LevenshteinUnsafe(string s, string t)
    {
        int lenS = s.Length;
        int lenT = t.Length;

        if (lenS == 0) return lenT;
        if (lenT == 0) return lenS;

        int* prev = stackalloc int[lenT + 1];
        int* curr = stackalloc int[lenT + 1];

        for (int j = 0; j <= lenT; j++)
            prev[j] = j;

        for (int i = 0; i < lenS; i++)
        {
            curr[0] = i + 1;
            char sChar = s[i]; // Cache current character for faster access
            for (int j = 0; j < lenT; j++)
            {
                int cost = sChar == t[j] ? 0 : 1; // Avoid redundant indexing
                curr[j + 1] = Math.Min(Math.Min(curr[j] + 1, prev[j + 1] + 1), prev[j] + cost);
            }
            var temp = prev;
            prev = curr;
            curr = temp;
        }
        return prev[lenT];
    }
}
