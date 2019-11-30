using System;
using System.Collections.Generic;
using System.Text;

namespace SevenZip.EventArguments
{
    /// <summary>
    /// Event args that pass a ulong total and a ulong completed value, that can be interpreted by the receiver.
    /// </summary>
    public sealed class DetailedProgressEventArgs : EventArgs
    {
        private readonly ulong _amountedCompleted;
        private readonly ulong _total;

        /// <summary>
        /// Initializes a new instance of the DetailedProgressEventArgs class.
        /// </summary>
        /// <param name="amountCompleted">Amount of work that has been cumulatively completed.</param>
        /// <param name="total">The total amount of work to complete.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        public DetailedProgressEventArgs(ulong amountCompleted, ulong total)
        {
            if (amountCompleted < 0 || amountCompleted > total)
            {
                throw new ArgumentOutOfRangeException("amountCompleted",
                    "The amount of completed work (" + amountCompleted + ") must be less than the total (" + total + ").");
            }

            _amountedCompleted = amountCompleted;
            _total = total;
        }

        /// <summary>
        /// Gets the amount of work that has been completed.
        /// </summary>
        public ulong AmountCompleted => _amountedCompleted;
        /// <summary>
        /// Gets the total amount of work to do.
        /// </summary>
        public ulong TotalAmount => _total;
    }
}
