using System;
using System.Collections.Generic;
using System.Linq;

namespace SevenZip
{
    public static class IEnumerableHelper
    {
        // from https://stackoverflow.com/a/3562370, variable names renamed.
        public static int IndexOfSequence<T>(this IEnumerable<T> source, IEnumerable<T> sequence, IEqualityComparer<T> comparer = null)
        {
            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            var seq = sequence.ToArray();
            int positionInSource = 0; // current position in source sequence
            int posInSearched = 0; // current position in searched sequence
            var possibleStartIndices = new List<int>(); // a list of possible start indices of the sequence in the source

            foreach (var item in source)
            {
                // Remove bad prospective matches
                possibleStartIndices.RemoveAll(possibleStartIndex => !comparer.Equals(item, seq[positionInSource - possibleStartIndex]));

                // Is it the start of a prospective match ?
                if (comparer.Equals(item, seq[0]))
                {
                    possibleStartIndices.Add(positionInSource);
                }

                // Does current character continues partial match ?
                if (comparer.Equals(item, seq[posInSearched]))
                {
                    posInSearched++;
                    // Do we have a complete match ?
                    if (posInSearched == seq.Length)
                    {
                        // Bingo !
                        return positionInSource - seq.Length + 1;
                    }
                }
                else // Mismatch
                {
                    // Do we have prospective matches to fall back to ?
                    if (possibleStartIndices.Count > 0)
                    {
                        // Yes, use the first one
                        int possibleStartIndex = possibleStartIndices[0];
                        posInSearched = positionInSource - possibleStartIndex + 1;
                    }
                    else
                    {
                        // No, start from beginning of searched sequence
                        posInSearched = 0;
                    }
                }

                positionInSource++;
            }

            // No match
            return -1;
        }
    }
}