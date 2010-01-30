//
// ConfigurationSchema.cs
//
// Authors:
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//   Patrick van Staveren  <trick@vanstaveren.us>
//
// Copyright (C) 2008-2009 Bertrand Lorentz and Patrick van Staveren.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Banshee.Configuration;

namespace Banshee.AlarmClock
{
    public static class ConfigurationSchema
    {
        private const string conf_namespace = "plugins.alarm";
        
        public static readonly SchemaEntry<bool> IsEnabled 
            = new SchemaEntry<bool> (
                conf_namespace, "is_enabled", 
                false, "Enable the Alarm plugin",
                ""
            );

        public static readonly SchemaEntry<int> AlarmHour 
            = new SchemaEntry<int> (
                conf_namespace, "alarm_hour", 
                0, "The hour at which the alarm goes off",
                ""
            );

        public static readonly SchemaEntry<int> AlarmMinute 
            = new SchemaEntry<int> (
                conf_namespace, "alarm_minute", 
                0, "The minute at which the alarm goes off",
                ""
            );

        public static readonly SchemaEntry<string> AlarmCommand 
            = new SchemaEntry<string> (
                conf_namespace, "alarm_command", 
                "", "The command executed when the alarm goes off",
                ""
            );

        public static readonly SchemaEntry<int> FadeStartVolume 
            = new SchemaEntry<int> (
                conf_namespace, "fade_start_volume", 
                0, "The volume level at which the alarm starts",
                ""
            );

        public static readonly SchemaEntry<int> FadeEndVolume 
            = new SchemaEntry<int> (
                conf_namespace, "fade_end_volume", 
                100, "The volume level at which the alarm ends",
                ""
            );

        public static readonly SchemaEntry<int> FadeDuration 
            = new SchemaEntry<int> (
                conf_namespace, "fade_duration", 
                60, "Duration of the volume fade",
                ""
            );
    }
}
