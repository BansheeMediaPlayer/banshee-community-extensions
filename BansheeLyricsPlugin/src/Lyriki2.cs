
// Liryki2.cs created with MonoDevelop
// User: sgrang at 23:20 27/08/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Text.RegularExpressions;
using Mono.Unix;
using System.Threading;
using System.IO;
using System.Net;
using System.Text;
namespace Banshee.Plugins.Lyrics
{
public class Lyriki2 :Banshee.Plugins.Lyrics.LyricBaseSource
{
	private string lyricURL="http://www.lyriki.com";
	//private string lyric_not_found="Unable to find Lyrics. <br> Suggestions :";
	
	public override string Name { get { return "<Lyriki> www.lyriki.com"; }}
    public override string Url { get { return lyricURL; }}
    public Lyriki2(){
			can_add=false;
	}
	private string ReadPageContent(String Url){
		//use always absolute url
        if (!Url.Contains(lyricURL))
            Url=lyricURL+Url;
        Console.WriteLine("loading url: "+Url);
        string html=null;
        try{
            html = base.GetSource(Url);
        }catch(Exception e){
        	Console.WriteLine("unable to contact server!"+e.Message);
            return "Unable to contact server!";
        }
		return html;
	}
    public override string GetLyrics(String Url){
		string lyric=null;
        string html=ReadPageContent(Url);
		//regular expression
        Regex r = new Regex ("<p>(.*?)</p><!",
                             RegexOptions.Multiline|RegexOptions.IgnoreCase |
                             RegexOptions.Singleline);
    	if (r.IsMatch (html)) {
        	Match m = r.Match(html);
        	lyric = m.Groups[1].ToString();
        }
    	return lyric;
    }
    public override string GetSuggestions(string artist,string title){
		
			string url = string.Format(lyricURL + "/{0}:{1}",System.Web.HttpUtility.UrlEncode(artist),
					                            System.Web.HttpUtility.UrlEncode(BansheeWidgets.CurrentTrack.GetAlbum()));
			return GetLyrics(url.Replace("+","_"));
	}
  public override string GetLyrics(string artist, string title)
    {
       	string url = string.Format(lyricURL +"/{0}:{1}",System.Web.HttpUtility.UrlEncode(artist),
			                       System.Web.HttpUtility.UrlEncode(title));
		//obtain the lyric from the given url
    	string lyric= GetLyrics(url.Replace("+","_"));
		
		return lyric;
    }
public override void AddLyrics(string artist,string title,string album, string year,string lyric){
			// Create a request using a URL that can receive a post. 
            WebRequest request = WebRequest.Create (lyricURL+string.Format("/index.php?title={0}:{1}&amp;action=submit",artist,title));
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Create POST data and convert it to a byte array.
			
            string postdata ="&wpTextbox1="+lyric;
			postdata=postdata+"&wpSummary="+album;
            byte[] byteArray = Encoding.UTF8.GetBytes (postdata);
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream ();
            // Write the data to the request stream.
            dataStream.Write (byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close ();
            // Get the response.
            WebResponse response = request.GetResponse ();
            // Display the status.
            Console.WriteLine (((HttpWebResponse)response).StatusDescription);
            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream ();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader (dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd ();
            // Display the content.
            Console.WriteLine (responseFromServer);
            // Clean up the streams.
            reader.Close ();
            dataStream.Close ();
            response.Close ();
    }
    public override string GetCredits ()
    {
        return string.Format("Powered by {0} (<a href=\"{1}\">{2}</a>)",
                             this.Name,
                             this.Url,
                             this.Url);
    }
  }
}