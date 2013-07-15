//
// ResultsPage.cs
//
// Author:
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright 2013 Tomasz Maczyński
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
using Hyena.Json;
using System.Collections.Generic;

namespace Banshee.SongKick.Recommendations
{
    public class ResultsPage<T> where T : Result
    {
        public bool IsWellFormed { get; private set; }
        public bool IsStatusOk { 
            get { return (status == "ok"); }
        }
        public string status { get; private set; }
        public Results<T> results { get; private set; }
        public ResultsError error { get; private set; }

        public delegate Results<T> GetResultsDelegate(JsonObject o);

        public ResultsPage (string answer, GetResultsDelegate getResultsDelegate)
        {
            this.IsWellFormed = false;

            try {
                var jsonObject = JsonObject.FromString (answer);

                JsonObject resultsPage = null;
                try {
                    resultsPage = jsonObject.Get <JsonObject> ("resultsPage");
                    this.status = resultsPage.Get <String> ("status");
                    if (IsStatusOk) {
                        // TODO: refactor
                        var hasAnyResults = resultsPage.Get <int>("totalEntries") != 0;
                        if (hasAnyResults) {
                            JsonObject resultsJson = resultsPage.Get <JsonObject> ("results");
                            if (resultsJson != null)  {
                                this.results = getResultsDelegate(resultsJson);
                            } else {
                                Hyena.Log.Error ("SongKick: Server returned 'ok' status without 'results'");
                            }
                        }
                        else {
                            this.results = new Results<T>();
                        }
                    }

                    var errorJson = resultsPage.Get <JsonObject> ("error");
                    if (errorJson != null)
                    {
                        this.error = new ResultsError(errorJson);
                    }

                    this.IsWellFormed = true;
                } catch (ArgumentNullException e) {
                    Hyena.Log.DebugException (e);
                }

                Hyena.Log.Debug(resultsPage.ToString());

            } catch (ApplicationException e) {
                Hyena.Log.DebugException(e);
            }
        }
    }
}

