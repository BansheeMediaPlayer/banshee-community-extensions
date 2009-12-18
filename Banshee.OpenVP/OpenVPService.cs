// 
// OpenVPService.cs
//  
// Author:
//       Chris Howie <cdhowie@gmail.com>
// 
// Copyright (c) 2009 Chris Howie
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;

using Banshee.ServiceStack;
using Banshee.NowPlaying;
using Banshee.Sources;

namespace Banshee.OpenVP
{
	public class OpenVPService : IExtensionService, IDisposable
	{
        private OpenVPSourceContents contents;
        
		public OpenVPService () { }
        
		#region IExtensionService implementation
        
		public void Initialize ()
		{
		    contents = new OpenVPSourceContents();
            
            ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            
            NowPlayingSource nps = ServiceManager.SourceManager.FindSources<NowPlayingSource>().FirstOrDefault();
            if (nps != null)
                nps.SetReplacementAudioContents(contents);
		}
        
        private void OnSourceAdded (SourceAddedArgs args)
        {
            NowPlayingSource nps = args.Source as NowPlayingSource;
            if (nps != null)
                nps.SetReplacementAudioContents(contents);
        }
		
		#endregion
        
		#region IDisposable implementation
        
		public void Dispose ()
		{
            NowPlayingSource nps = ServiceManager.SourceManager.FindSources<NowPlayingSource>().FirstOrDefault();
            if (nps != null)
                nps.SetReplacementAudioContents(null);
            
		    contents.Destroy ();
		}
		
		#endregion
        
		#region IService implementation
        
		public string ServiceName {
		    get {
		        return "OpenVPService";
		    }
		}
		
		#endregion
	}
}
