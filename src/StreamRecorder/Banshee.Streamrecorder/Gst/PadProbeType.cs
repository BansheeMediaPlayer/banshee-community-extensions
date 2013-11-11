//
// Pad.cs
//
// Author:
//   Frank Ziegler
//
// Copyright (C) 2009 Frank Ziegler
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

namespace Banshee.Streamrecorder.Gst
{
	public enum PadProbeType
	{
          GST_PAD_PROBE_TYPE_INVALID          = 0,
          /* flags to control blocking */
          GST_PAD_PROBE_TYPE_IDLE             = (1 << 0),
          GST_PAD_PROBE_TYPE_BLOCK            = (1 << 1),
          /* flags to select datatypes */
          GST_PAD_PROBE_TYPE_BUFFER           = (1 << 4),
          GST_PAD_PROBE_TYPE_BUFFER_LIST      = (1 << 5),
          GST_PAD_PROBE_TYPE_EVENT_DOWNSTREAM = (1 << 6),
          GST_PAD_PROBE_TYPE_EVENT_UPSTREAM   = (1 << 7),
          GST_PAD_PROBE_TYPE_EVENT_FLUSH      = (1 << 8),
          GST_PAD_PROBE_TYPE_QUERY_DOWNSTREAM = (1 << 9),
          GST_PAD_PROBE_TYPE_QUERY_UPSTREAM   = (1 << 10),
          /* flags to select scheduling mode */
          GST_PAD_PROBE_TYPE_PUSH             = (1 << 12),
          GST_PAD_PROBE_TYPE_PULL             = (1 << 13),

          /* flag combinations */
          GST_PAD_PROBE_TYPE_BLOCKING         = GST_PAD_PROBE_TYPE_IDLE | GST_PAD_PROBE_TYPE_BLOCK,
          GST_PAD_PROBE_TYPE_DATA_DOWNSTREAM  = GST_PAD_PROBE_TYPE_BUFFER | GST_PAD_PROBE_TYPE_BUFFER_LIST | GST_PAD_PROBE_TYPE_EVENT_DOWNSTREAM,
          GST_PAD_PROBE_TYPE_DATA_UPSTREAM    = GST_PAD_PROBE_TYPE_EVENT_UPSTREAM,
          GST_PAD_PROBE_TYPE_DATA_BOTH        = GST_PAD_PROBE_TYPE_DATA_DOWNSTREAM | GST_PAD_PROBE_TYPE_DATA_UPSTREAM,
          GST_PAD_PROBE_TYPE_BLOCK_DOWNSTREAM = GST_PAD_PROBE_TYPE_BLOCK | GST_PAD_PROBE_TYPE_DATA_DOWNSTREAM,
          GST_PAD_PROBE_TYPE_BLOCK_UPSTREAM   = GST_PAD_PROBE_TYPE_BLOCK | GST_PAD_PROBE_TYPE_DATA_UPSTREAM,
          GST_PAD_PROBE_TYPE_EVENT_BOTH       = GST_PAD_PROBE_TYPE_EVENT_DOWNSTREAM | GST_PAD_PROBE_TYPE_EVENT_UPSTREAM,
          GST_PAD_PROBE_TYPE_QUERY_BOTH       = GST_PAD_PROBE_TYPE_QUERY_DOWNSTREAM | GST_PAD_PROBE_TYPE_QUERY_UPSTREAM,
          GST_PAD_PROBE_TYPE_ALL_BOTH         = GST_PAD_PROBE_TYPE_DATA_BOTH | GST_PAD_PROBE_TYPE_QUERY_BOTH,
          GST_PAD_PROBE_TYPE_SCHEDULING       = GST_PAD_PROBE_TYPE_PUSH | GST_PAD_PROBE_TYPE_PULL
	}

}
