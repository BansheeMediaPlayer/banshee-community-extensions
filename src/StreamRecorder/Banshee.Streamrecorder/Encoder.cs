//
// Encoder.cs
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

namespace Banshee.Streamrecorder
{
    public class Encoder
    {

        private string name;
        private string pipeline;
        private string file_extension;
        private bool is_preferred;

        public Encoder (string name, string pipeline, string file_extension) : this(name, pipeline, file_extension, false)
        {
        }

        public Encoder (string name, string pipeline, string file_extension, bool is_preferred)
        {
            this.name = name;
            this.pipeline = pipeline;
            this.file_extension = file_extension;
            this.is_preferred = is_preferred;
        }

        public string Name {
            get { return name; }
            set { name = value; }
        }

        public string Pipeline {
            get { return pipeline; }
            set { pipeline = value; }
        }

        public string FileExtension {
            get { return file_extension; }
            set { file_extension = value; }
        }

        public bool IsPreferred {
            get { return is_preferred; }
            set { is_preferred = value; }
        }

        public override string ToString ()
        {
            return name;
        }
        
    }
    
}
