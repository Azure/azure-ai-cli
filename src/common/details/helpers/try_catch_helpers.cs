//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Threading;

namespace Azure.AI.Details.Common.CLI
{
    public class TryCatchHelpers
    {
        public static Exception TryCatchRetryNoThrow(Action action, int retryTimes = 10)
        {
            return TryCatchRetryNoThrow<Exception>(action, null, retryTimes);
        }

        public static Exception TryCatchRetryNoThrow(Action action, Func<Exception, bool> retryIf, int retryTimes = 10)
        {
            return TryCatchRetryNoThrow<Exception>(action, retryIf, retryTimes);
        }

        public static ExceptionType TryCatchRetryNoThrow<ExceptionType>(Action action, int retryTimes = 10) where ExceptionType : Exception
        {
            return TryCatchRetryNoThrow<ExceptionType>(action, null, retryTimes);
        }

        public static ExceptionType TryCatchRetryNoThrow<ExceptionType>(Action action, Func<ExceptionType, bool> retryIf = null, int retryTimes = 10) where ExceptionType : Exception
        {
            var retry = 0;
            while (retry <= retryTimes)
            {
                try
                {
                    action();
                    break;
                }
                catch (ExceptionType ex)
                {
                    if (retry++ > retryTimes) return ex;
                    if (retryIf != null && !retryIf(ex)) return ex;
                    Thread.Sleep(retry * 22);
                }
            }

            return null;
        }

        public static void TryCatchRetry(Action action, int retryTimes = 10)
        {
            var ex = TryCatchRetryNoThrow(action, retryTimes);
            if (ex != null) throw ex;
        }

        public static void TryCatchRetry(Action action, Func<Exception, bool> retryIf, int retryTimes = 10)
        {
            var ex = TryCatchRetryNoThrow(action, retryIf, retryTimes);
            if (ex != null) throw ex;
        }

        public static void TryCatchRetry<ExceptionType>(Action action, int retryTimes = 10) where ExceptionType : Exception
        {
            var ex = TryCatchRetryNoThrow<ExceptionType>(action, retryTimes);
            if (ex != null) throw ex;
        }

        public static void TryCatchRetry<ExceptionType>(Action action, Func<ExceptionType, bool> retryIf = null, int retryTimes = 10) where ExceptionType : Exception
        {
            var ex = TryCatchRetryNoThrow<ExceptionType>(action, retryIf, retryTimes);
            if (ex != null) throw ex;
        }

        public static T TryCatchNoThrow<T>(Func<T> function, T defaultResult, out Exception functionThrewException)
        {
            functionThrewException = null;
            try
            {
                return function();
            }
            catch (Exception ex)
            {
                functionThrewException = ex;
            }
            return defaultResult;
        }
    }
}
