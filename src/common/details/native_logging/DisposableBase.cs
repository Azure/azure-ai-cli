using System;
using System.Threading;

namespace Azure.AI.Details.Common.CLI
{
    internal abstract class DisposableBase : IDisposable
    {
        private int isDisposed;

        /// <summary>
        /// The base destructor
        /// </summary>
        ~DisposableBase()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Indicates whether this object has been disposed.
        /// </summary>
        public virtual bool IsDisposed
        {
            get
            {
                return this.isDisposed == 1;
            }
        }

        /// <summary>
        /// Disposes the current object. Safe to call multiple times.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.isDisposed, 1) == 0)
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Method that actually disposes of resources.
        /// </summary>
        /// <param name="disposeManaged">True if we should dispose both managed and native
        /// resources, false if we should only dispose native resources</param>
        protected abstract void Dispose(bool disposeManaged);

        /// <summary>
        /// Throws an exception if the object has been disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">If the object has been disposed</exception>
        protected virtual void CheckDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}