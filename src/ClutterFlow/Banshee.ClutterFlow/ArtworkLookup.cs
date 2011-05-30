//
// ArtworkLookup.cs
//
// Author:
//       Mathijs Dumon <mathijsken@hotmail.com>
//
// Copyright (c) 2010 Mathijs Dumon
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
using System.Threading;
using System.Collections.Generic;

using Gdk;
using Clutter;

using Banshee.ServiceStack;
using Banshee.Collection.Gui;
using Hyena;
using Hyena.Collections;

using ClutterFlow;

namespace Banshee.ClutterFlow
{

    /// <summary>
    /// The ArtworkLookup class is an asynchronous worker thread processing artwork lookups.
    /// </summary>
    public class ArtworkLookup : IDisposable {

        #region Fields
        private ArtworkManager artwork_manager;

        private CoverManager cover_manager;

        public object SyncRoot {
            get { return LookupQueue.SyncRoot; }
        }
        private FloatingQueue<ClutterFlowAlbum> LookupQueue = new FloatingQueue<ClutterFlowAlbum> ();

        // Lock covering stopping and stopped
        protected readonly object stopLock = new object ();
        // Whether or not the worker thread has been asked to stop
        protected bool stopping = false;
        // Whether or not the worker thread has stopped
        protected bool stopped = true;

        /// <value>
        /// Returns whether the worker thread has been asked to stop.
        /// This continues to return true even after the thread has stopped.
        /// </value>
        public bool Stopping {
            get { lock (stopLock) { return stopping; } }
            protected set { lock (stopLock) { stopping = value; } }
        }

        //// <value>
        // Returns whether the worker thread has stopped.
        /// </value>
        public bool Stopped {
            get { return artwork_thread != null ? (artwork_thread.ThreadState == ThreadState.Stopped) : true; }
        }
        #endregion

        Thread artwork_thread;
        public ArtworkLookup (CoverManager cover_manager)
        {
            //Log.Debug ("ArtworkLookup ctor ()");
            this.cover_manager = cover_manager;
            this.cover_manager.TargetIndexChanged += HandleTargetIndexChanged;
            artwork_manager = ServiceManager.Get<ArtworkManager> ();
            artwork_manager.AddCachedSize (cover_manager.TextureSize);
        }

        #region Queueing and index hinting
        public void Enqueue (ClutterFlowAlbum cover)
        {
            if (!cover.Enqueued) {
                cover.Enqueued = true;
                LookupQueue.Enqueue (cover);
                Start ();
            }
        }

        private int new_focus = -1;
        private object focusLock = new object ();
        protected void HandleTargetIndexChanged(object sender, EventArgs e)
        {
            //Log.Debug ("ArtworkLookup HandleTargetIndexChanged locking focusLock");
            lock (focusLock) {
                //Log.Debug ("ArtworkLookup HandleTargetIndexChanged locked focusLock");
                new_focus = cover_manager.TargetIndex;
            }
        }
        #endregion

        #region  Start/Stop Handling
        protected bool disposed = false;
        public virtual void Dispose ()
        {
            if (disposed) {
                return;
            }
            disposed = true;

            cover_manager.TargetIndexChanged -= HandleTargetIndexChanged;

            Stop ();
            LookupQueue.Dispose ();
        }

        // Tells the worker thread to stop, typically after completing its
        // current work item. (The thread is *not* guaranteed to have stopped
        // by the time this method returns.)
        public void Stop ()
        {
            Log.Debug ("ArtworkLookup Stop ()");
            Stopping = true;
            if (SyncRoot != null) {
                lock (SyncRoot) {
                    Monitor.Pulse (SyncRoot);
                }
            }
            if (artwork_thread != null) {
                artwork_thread.Join ();
            }
        }

        public void Start ()
        {
            if (artwork_thread == null) {
                artwork_thread = new Thread (new ThreadStart (Run));
                artwork_thread.Priority = ThreadPriority.BelowNormal;
                stopping = false;
                artwork_thread.Start ();
            } else if (artwork_thread.ThreadState == ThreadState.Unstarted) {
                stopping = false;
                artwork_thread.Start ();
            }
        }
        #endregion

        // Main work loop of the class.
        private void Run ()
        {
            if (disposed) {
                return;
            }

            try {
                Log.Debug ("ArtworkLookup Run ()");
                while (!Stopping) {
                    //Log.Debug ("ArtworkLookup Run locking focusLock");
                    lock (focusLock) {
                        //Log.Debug ("ArtworkLookup Run locked focusLock");
                        if (new_focus>-1)
                            LookupQueue.Focus = new_focus;
                        new_focus = -1;
                    }

                    while (!Stopping && LookupQueue != null && LookupQueue.Count == 0) {
                        lock (SyncRoot) {
                            //Log.Debug ("ArtworkLookup Run - waiting for pulse");
                            bool ret = Monitor.Wait (SyncRoot, 5000);
                            if (!ret) Stopping = true;
                            //Log.Debug ("ArtworkLookup Run - pulsed");
                        }
                    }
                    if (Stopping) {
                        return;
                    }
                    ClutterFlowAlbum cover = LookupQueue.Dequeue ();
                    float size = cover_manager.TextureSize;
                    string cache_id = cover.PbId;
                    Cairo.ImageSurface surface = artwork_manager.LookupScaleSurface (cache_id, (int) size);
                    if (surface != null) {
                        Gtk.Application.Invoke (delegate {
                            SetCoverToSurface (cover, surface);
                        });
                    }
                }
            } finally {
               Log.Debug ("ArtworkLookup Run done");
            }
        }

        private void SetCoverToSurface (ClutterFlowAlbum cover, Cairo.ImageSurface surface)
        {
                cover.Enqueued = false;
                //cover.SwappedToDefault = false;
                ClutterFlowActor.MakeReflection (surface, cover.Cover);
        }
    }
}
