//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class FaceCommand : Command
    {
        internal FaceCommand(ICommandValues values) : base(values)
        {
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunFaceCommand();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "face"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunFaceCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "face.identify": DoFaceIdentify(); break;
                case "face.verify": DoFaceVerify(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private void DoFaceIdentify()
        {
            Console.WriteLine("DoFaceIdentify not yet implemted...");

            // new Azure.AI.Vision.FaceRecognition.FaceRecognizer(new VisionServiceOptions(new Uri(""), new VisionServiceAdvancedOptions()));
        }

        private void DoFaceVerify()
        {
            Console.WriteLine("DoFaceVerify not yet implemted...");
        }

        private bool _quiet = false;
        private bool _verbose = false;
    }
}
