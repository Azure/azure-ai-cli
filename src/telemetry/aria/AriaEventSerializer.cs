//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.Details.Common.CLI.Telemetry;
using Microsoft.Applications.Events;
using System.Reflection;
using PiiKind = Azure.AI.Details.Common.CLI.Telemetry.PiiKind;
using AriaPii = Microsoft.Applications.Events.PiiKind;

namespace Azure.AI.CLI.Telemetry.Aria
{
    [System.Diagnostics.DebuggerStepThrough]
    internal class AriaEventSerializer
    {
        public EventProperties Serialize(ITelemetryEvent evt)
        {
            if (evt == null)
            {
                throw new ArgumentNullException(nameof(evt));
            }

            var eventData = new EventProperties();
            eventData.Name = evt.Name;

            // TODO this could be optimized by generating and caching the dynamic code to do this generation
            // instead of needing to use reflection each time

            var publicProps = evt.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.Name != "Name");

            foreach (var prop in publicProps)
            {
                string name = prop.Name;
                AriaPii pii = ToAriaPII(prop.GetCustomAttribute<PiiAttribute>(true)?.Kind ?? PiiKind.None);
                object? value = prop.GetValue(evt);

                switch (value)
                {
                    case null:
                        // do nothing
                        break;

                    case string strValue:
                        eventData.SetProperty(name, strValue, pii);
                        break;

                    case bool boolValue:
                        eventData.SetProperty(name, boolValue, pii);
                        break;

                    case DateTime dtValue:
                        eventData.SetProperty(name, dtValue, pii);
                        break;

                    case DateTimeOffset dtValue:
                        eventData.SetProperty(name, dtValue.UtcDateTime, pii);
                        break;

                    case Guid guidValue:
                        eventData.SetProperty(name, guidValue, pii);
                        break;

                    case sbyte:
                    case short:
                    case int:
                    case byte:
                    case ushort:
                    case uint:
                    case long:
                        eventData.SetProperty(name, Convert.ToInt64(value), pii);
                        break;

                    case float:
                    case double:
                    case ulong:
                        eventData.SetProperty(name, Convert.ToDouble(value), pii);
                        break;

                    case Enum enumValue:
                        eventData.SetProperty(name, value.ToString(), pii);
                        break;

                    default:
                        // don't know how to handle this type so try to use the generic TypeConverter to get a string
                        var conv = System.ComponentModel.TypeDescriptor.GetConverter(prop.PropertyType);
                        string? strVal = conv.ConvertToInvariantString(value);
                        if (strVal != null)
                        {
                            eventData.SetProperty(name, strVal, pii);
                        }
                        break;
                }
            }

            return eventData;
        }

        private static AriaPii ToAriaPII(PiiKind kind)
        {
            switch (kind)
            {
                case PiiKind.None:       return AriaPii.None;
                case PiiKind.UserId:     return AriaPii.Identity;
                case PiiKind.IP4Address: return AriaPii.IPv4Address;
                case PiiKind.IP6Address: return AriaPii.IPv6Address;
                case PiiKind.Uri:        return AriaPii.Uri;

                // better to err on the side of caution and assume a higher PII
                default:                 return AriaPii.Identity;
            }
        }
    }
}
