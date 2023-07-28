//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;

namespace Azure.AI.Details.Common.CLI
{
    public class TextWriterReadLineHelper : TextWriter
    {
        public override Encoding Encoding => this.writer.Encoding;

        public async IAsyncEnumerable<string> ReadAllLinesAsync()
        {
            var sb = writer.GetStringBuilder();

            string line = String.Empty;
            while (!writerClosed || sb.Length > 0)
            {
                Monitor.Enter(this);
                for (var i = 0; i < sb.Length; i++)
                {
                    if (sb[i] == '\n')
                    {
                        line = sb.ToString(0, i + 1);
                        sb.Remove(0, i + 1);
                        break;
                    }
                }
                Monitor.Exit(this);

                if (line.Length > 0)
                {
                    yield return line.TrimEnd('\r', '\n');
                    line = String.Empty;
                }

                if (sb.Length > 0) continue;

                await Task.Run(() => { signal.WaitOne();});
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            Monitor.Enter(this);

            writer.Write(buffer, index, count);
            signal.Set();

            Monitor.Exit(this);
        }

#if NET
        public override void Write(System.ReadOnlySpan<char> buffer)
        {
            Monitor.Enter(this);

            writer.Write(buffer);
            signal.Set();

            Monitor.Exit(this);
        }
#endif

        public async override System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count)
        {
            Monitor.Enter(this);

            await writer.WriteAsync(buffer, index, count);
            signal.Set();

            Monitor.Exit(this);
        }

#if NET
        public async override System.Threading.Tasks.Task WriteAsync(System.ReadOnlyMemory<char> buffer, System.Threading.CancellationToken cancellationToken = default)
        {
            Monitor.Enter(this);

            await writer.WriteAsync(buffer, cancellationToken);
            signal.Set();

            Monitor.Exit(this);
        }
#endif

        protected override void Dispose(bool disposing)
        {
            writer.Dispose();
        }

#if NET
        public async override System.Threading.Tasks.ValueTask DisposeAsync()
        {
            await writer.DisposeAsync();
            await Task.CompletedTask;
        }
#endif

        public override void Close()
        {
            Monitor.Enter(this);

            writer.Close();
            writerClosed = true;

            signal.Set();

            Monitor.Exit(this);
        }

        bool writerClosed = false;
        readonly StringWriter writer = new StringWriter();

        readonly AutoResetEvent signal = new AutoResetEvent(false);
    }

}
