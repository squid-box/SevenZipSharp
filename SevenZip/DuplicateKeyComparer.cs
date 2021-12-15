using System;
using System.Collections.Generic;

namespace SevenZip
{
    // from http://stackoverflow.com/questions/5716423/c-sharp-sortable-collection-which-allows-duplicate-keys/21886340#21886340
    public class DuplicateKeyComparer<TKey>
        : IComparer<TKey>
        where TKey : IComparable
    {
        #region Fields

        private readonly bool m_descending;

        #endregion

        #region Constructors

        public DuplicateKeyComparer(bool i_descending = false)
        {
            m_descending = i_descending;
        }

        #endregion

        #region Public Methods

        public virtual int Compare(TKey x, TKey y)
        {
            int result = m_descending
                ? y.CompareTo(x)
                : x.CompareTo(y);

            if (result == 0)
                return 1; // Handle equality as being greater

            return result;
        }

        #endregion
    }
}