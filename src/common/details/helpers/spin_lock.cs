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
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public class SpinLock
    {
        public void StartLock()
        {
            Interlocked.Increment(ref _lockCounter);
        }

        public void StopLock()
        {
            StopLock(int.MaxValue);
        }

        public void StopLock(int maxWait)
        {
            while (maxWait > 0)
            {
                var replaced = Interlocked.CompareExchange(ref _lockCounter, 0, 1);
                if (1 == replaced && _lockCounter == 0) break;

                Thread.Sleep(10);
                maxWait -= 10;
            }
        }

        public void EnterReaderLock()
        {
            Interlocked.Increment(ref _lockCounter);
        }

        public void ExitReaderLock()
        {
            Interlocked.Decrement(ref _lockCounter);
        }

        public void EnterReaderLockOnce(ref int onceCounter)
        {
            var replaced = Interlocked.CompareExchange(ref onceCounter, 1, 0);
            if (0 == replaced && onceCounter == 1) EnterReaderLock();
        }

        public void ExitReaderLockOnce(ref int onceCounter)
        {
            var replaced = Interlocked.CompareExchange(ref onceCounter, 0, 1);
            if (1 == replaced && onceCounter == 0) ExitReaderLock();
        }

        private int _lockCounter = 0;
    }
}
