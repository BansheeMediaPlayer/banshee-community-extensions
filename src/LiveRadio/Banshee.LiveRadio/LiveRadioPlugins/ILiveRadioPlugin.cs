//
// ILiveRadioPlugin.cs
//
// Authors:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (C) 2010 Frank Ziegler
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

using System.Collections.Generic;
using Hyena;

namespace Banshee.LiveRadio.Plugins
{

    /// <summary>
    /// Public interface any LiveRadio plugin must implement
    /// </summary>
    public interface ILiveRadioPlugin
    {
        /// <summary>
        /// Event raised when a genre list has been retrieved by the plugin
        /// </summary>
        event GenreListLoadedEventHandler GenreListLoaded;
        /// <summary>
        /// Event raised when a query result has been retrieved by the plugin
        /// </summary>
        event RequestResultRetrievedEventHandler RequestResultRetrieved;
        /// <summary>
        /// Event raised when an Error occurs in a plugin
        /// </summary>
        event ErrorReturnedEventHandler ErrorReturned;

        /// <summary>
        /// Return a string most likely unique to the plugin, best practice is
        /// to return the name of the plugin
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> -- a string representation of the object
        /// </returns>
        string ToString ();

        /// <summary>
        /// Initializes the plugin, usually will result in initiating the retrieval of
        /// the genre list
        /// Must set Enabled Property to true
        /// </summary>
        void Initialize ();

        /// <summary>
        /// Method that will disable the plugin. Must set Enabled Property to false and should
        /// disable any background tasks
        /// </summary>
        void Disable ();

        /// <summary>
        /// Method that will be called when the genre list needs to be filled and a
        /// GenreListLoaded event will be expected to follow this function call
        /// </summary>
        void RetrieveGenreList ();

        /// <summary>
        /// Method that will be called when a user request needs to be processed and
        /// the cached_results object needs to be filled or queried. A RequestResultsRetrieved
        /// event is expected to follow this function call
        /// </summary>
        /// <param name="request_type">
        /// A <see cref="LiveRadioRequestType"/> -- the type of the request
        /// </param>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the freetext query or the genre name
        /// </param>
        void ExecuteRequest (LiveRadioRequestType request_type, string query);

        /// <summary>
        /// Sets the LiveRadioPluginSource for this plugin. The plugin must save a reference to
        /// the source object and return it upon request through the PluginSource Property
        /// </summary>
        /// <param name="source">
        /// A <see cref="LiveRadioPluginSource"/> -- the source
        /// </param>
        void SetLiveRadioPluginSource (LiveRadioPluginSource source);

        /// <summary>
        /// Cleans up any plugin specific data from a track url, such as session data or any other
        /// temporary parameters.
        /// </summary>
        /// <param name="url">
        /// A <see cref="SafeUri"/> -- the original url to be cleaned
        /// </param>
        /// <returns>
        /// A <see cref="SafeUri"/> -- the cleaned url
        /// </returns>
        SafeUri CleanUpUrl (SafeUri url);

        SafeUri RetrieveUrl (string baseurl);

        /// <summary>
        /// Actual method that does the work of saving the configuration for this plugin. For examples see derived
        /// classes. There are no predefined SchemaEntry objects, the derived class needs to take care of those.
        /// </summary>
        void SaveConfiguration ();

        /// <summary>
        /// Returns the configuration widget for the plugin or null if there is no configuration
        /// </summary>
        Gtk.Widget ConfigurationWidget { get; }

        /// <summary>
        /// Returns the LiveRadioPluginSource for this plugin
        /// </summary>
        LiveRadioPluginSource PluginSource { get; }

        /// <summary>
        /// Returns a plugin name unique to the implementing plugin
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns the list of genres
        /// </summary>
        List<Genre> Genres { get; }

        /// <summary>
        /// Must truthfully return, if the plugin is enabled. A plugin is enabled, if it has been initialized
        /// and not been disabled afterwards
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Should return "Yes" if enabled, and "No" if not enabled
        /// </summary>
        string IsEnabled { get; }

        /// <summary>
        /// Returns the version information for the plugin
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Returns, wether the plugin can be added to the LiveRadio plugin list
        /// </summary>
        bool Active { get; }
    }
}
