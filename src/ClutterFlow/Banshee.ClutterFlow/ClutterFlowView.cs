//
// ClutterFlowView.cs
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
using System.Collections.Generic;

using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Collection;
using Hyena.Data;
using Hyena.Gui;

using Gdk;
using Gtk;
using Cairo;
using Pango;
using Clutter;
using ClutterFlow;
using ClutterFlow.Captions;
using ClutterFlow.Slider;
using ClutterFlow.Alphabet;

using Banshee.ClutterFlow.Buttons;

namespace Banshee.ClutterFlow
{

    public class ClutterFlowView : Clutter.Embed
    {
        #region Fields
        #region Active/Current Album Related
        public event EventHandler UpdatedAlbum;

        private AlbumInfo activeAlbum = null;
        public AlbumInfo ActiveAlbum {
            get { return activeAlbum; }
            protected set { activeAlbum = value; }
        }
        private int activeIndex = -1;
        public int ActiveIndex {
            get { return activeIndex; }
            protected set { activeIndex = value; }
        }
        public int ActiveModelIndex {
            get {
                int ret = AlbumLoader.ConvertIndexToModelIndex (ActiveIndex);
                Hyena.Log.DebugFormat ("ActiveModelIndex_get will return {0}", ret);
                return ret;
            }
        }

        public AlbumInfo CurrentAlbum {
            get {
                var album_actor = cover_manager.CurrentCover as ClutterFlowAlbum;
                if (album_actor != null) {
                    return album_actor.Album;
                } else {
                    return null;
                }
            }
        }

        public int CurrentIndex {
            get {
                var album_actor = cover_manager.CurrentCover as ClutterFlowAlbum;
                if (album_actor != null) {
                    return album_actor.Index;
                } else {
                    return -1;
                }
            }
        }

        public int CurrentModelIndex {
            get { return AlbumLoader.ConvertIndexToModelIndex (CurrentIndex); }
        }
        #endregion

        #region General
        private AlbumLoader album_loader;
        public AlbumLoader AlbumLoader {
            get { return album_loader; }
        }
        private CoverManager cover_manager;
        public CoverManager CoverManager {
            get { return cover_manager; }
        }

        private int model_count;

        public virtual IListModel<AlbumInfo> Model {
            get { return album_loader.Model; }
        }

        public void SetModel (FilterListModel<AlbumInfo> value)
        {
            if (value != album_loader.Model) {
                if (album_loader.Model != null) {
                    album_loader.Model.Cleared -= OnModelClearedHandler;
                    album_loader.Model.Reloaded -= OnModelReloadedHandler;
                }

                album_loader.Model = value;

                if (album_loader.Model != null) {
                    album_loader.Model.Cleared += OnModelClearedHandler;
                    album_loader.Model.Reloaded += OnModelReloadedHandler;
                    model_count = album_loader.Model.Count;
                }
                CoverManager.ReloadCovers ();
            }
        }

        protected bool attached = false;
        public bool Attached {
            get { return attached; }
        }
        #endregion

        #region User Interface & Interaction
        private bool dragging = false;            // wether or not we are currently dragging the viewport around
        private double mouse_x, mouse_y;
        private float drag_x0, drag_y0;        // initial coordinates when the mouse button was pressed down
        private int start_index;

        private float drag_sens = 0.3f;
        public float DragSensitivity {
            get { return drag_sens; }
            set {
                if (value < 0.01f) {
                    value = 0.01f;
                }
                if (value > 2.0f) {
                    value = 2.0f;
                }
                drag_sens = value;
            }
        }

        private ClutterFlowSlider slider;
        public ClutterFlowSlider Slider {
            get { return slider;    }
        }
        private ClutterWidgetBar widget_bar;
        public ClutterWidgetBar WidgetBar {
            get { return widget_bar;    }
        }
        private PartyModeButton pm_button;
        public PartyModeButton PMButton {
            get { return pm_button;    }
        }
        private FullscreenButton fs_button;
        public FullscreenButton FSButton {
            get { return fs_button; }
        }
        private SortButton sort_button;
        public SortButton SortButton {
            get { return sort_button; }
        }
        private CoverCaption caption_cover;
        public CoverCaption LabelCover {
            get { return caption_cover;    }
        }
        private TrackCaption caption_track;
        public TrackCaption LabelTrack {
            get { return caption_track;    }
        }

        public bool LabelCoverIsVisible {
            set {
                if (value) {
                    caption_cover.ShowAll();
                } else {
                    caption_cover.HideAll();
                }
            }
        }
        public bool LabelTrackIsVisible {
            set { if (value) {
                    caption_track.ShowAll();
                } else {
                    caption_track.HideAll();
                }
            }
        }


        private const float rotSens = 0.00001f;
        private const float viewportMaxAngleX = 10; // maximum X viewport angle
        private const float viewportMinAngleX = -30; // maximum X viewport angle
        private const float viewportMaxAngleY = -15; // maximum Y viewport angle
        private float viewportAngleX = -5f;             // current X viewport angle
        public float ViewportAngleX {
            get { return viewportAngleX; }
            set {
                if (value != viewportAngleX) {
                    viewportAngleX = value;
                    if (viewportAngleX < viewportMinAngleX) {
                        viewportAngleX = viewportMinAngleX;
                    }
                    if (viewportAngleX > viewportMaxAngleX) {
                        viewportAngleX = viewportMaxAngleX;
                    }

                    if (viewportAngleX < -1f && viewportAngleX > -9f) {
                        cover_manager.SetRotation(RotateAxis.Y, -5f, cover_manager.Width*0.5f,cover_manager.Height*0.5f,cover_manager.Behaviour.ZFar);
                    } else {
                        cover_manager.SetRotation(RotateAxis.X, viewportAngleX, cover_manager.Width*0.5f,cover_manager.Height*0.5f,cover_manager.Behaviour.ZFar);
                    }
                }
            }
        }
        private float viewportAngleY = 0;            // current Y viewport angle
        public float ViewportAngleY {
            get { return viewportAngleY; }
            set {
                if (value != viewportAngleY) {
                    viewportAngleY = value;
                    if (viewportAngleY < viewportMaxAngleY) {
                        viewportAngleY = viewportMaxAngleY;
                    }
                    if (viewportAngleY > -viewportMaxAngleY) {
                        viewportAngleY = -viewportMaxAngleY;
                    }

                    if (viewportAngleY > -4f && viewportAngleY < 4f) {
                        cover_manager.SetRotation(RotateAxis.Y, 0, cover_manager.Width*0.5f,cover_manager.Height*0.5f,cover_manager.Behaviour.ZFar);
                    } else {
                        cover_manager.SetRotation(RotateAxis.Y, viewportAngleY, cover_manager.Width*0.5f,cover_manager.Height*0.5f,cover_manager.Behaviour.ZFar);
                    }
                }
            }
        }
        #endregion
        #endregion

        #region Initialisation
        public ClutterFlowView () : base ()
        {
            SetSizeRequest (300, 200);
            Clutter.Global.MotionEventsEnabled = true;

            album_loader = new AlbumLoader ();
            cover_manager = new CoverManager (album_loader, GetDefaultSurface, ClutterFlowSchemas.TextureSize.Get ());

            AttachEvents ();

            SetupViewport ();
            SetupSlider ();
            SetupLabels ();
            SetupWidgetBar ();
        }

        private void AttachEvents ()
        {
            if (attached) {
                return;
            }
            attached = true;

            Stage.AllocationChanged += HandleAllocationChanged;
            Stage.ScrollEvent += HandleScroll;
            Stage.ButtonReleaseEvent += HandleButtonReleaseEvent;
            Stage.ButtonPressEvent += HandleButtonPressEvent;
            Stage.MotionEvent += HandleMotionEvent;
            cover_manager.ActorActivated += HandleActorActivated;
        }

        private void DetachEvents ()
        {
            if (!attached)
                return;

            Stage.AllocationChanged -= HandleAllocationChanged;
            Stage.ScrollEvent -= HandleScroll;
            Stage.ButtonReleaseEvent -= HandleButtonReleaseEvent;
            Stage.ButtonPressEvent -= HandleButtonPressEvent;
            Stage.MotionEvent -= HandleMotionEvent;
            cover_manager.ActorActivated -= HandleActorActivated;

            attached = false;
        }

        protected bool disposed = false;
        public override void Dispose ()
        {
            if (disposed) {
                return;
            }
            disposed = true;

            DetachEvents ();
            AlbumLoader.Dispose ();
            CoverManager.Dispose ();
        }


        protected void SetupViewport ()
        {
            Stage.Color = new Clutter.Color (0x00, 0x00, 0x00, 0xff);
            cover_manager.SetRotation (RotateAxis.X, viewportAngleX, Stage.Width/2, Stage.Height/2,0);
            Stage.Add (cover_manager);

            cover_manager.EmptyActor.SetToPb (
                IconThemeUtils.LoadIcon (cover_manager.TextureSize, "gtk-stop", "clutterflow-large.png")
            );
            CoverManager.DoubleClickTime = (uint) Gtk.Settings.GetForScreen (this.Screen).DoubleClickTime;
            cover_manager.LowerBottom ();
            cover_manager.Show ();
        }

        protected void SetupSlider ()
        {
            slider = new ClutterFlowSlider (400, 40, cover_manager);
            Stage.Add (slider);
        }

        protected void SetupLabels () {
            caption_cover = new CoverCaption (cover_manager, "Sans Bold 10", new Clutter.Color(1.0f,1.0f,1.0f,1.0f));
            Stage.Add (caption_cover);

            caption_track = new TrackCaption (cover_manager, "Sans Bold 10", new Clutter.Color(1.0f,1.0f,1.0f,1.0f));
            Stage.Add (caption_track);
        }

        protected void SetupWidgetBar ()
        {
            pm_button = new PartyModeButton ();
            fs_button = new FullscreenButton ();
            sort_button = new SortButton ();

            widget_bar = new ClutterWidgetBar (new Actor[] { pm_button, fs_button, sort_button });
            widget_bar.ShowAll ();
            Stage.Add (widget_bar);
            widget_bar.SetPosition (5, 5);
        }
        #endregion

        #region Rendering
        //Update all elements:
        protected void RedrawInterface ()
        {
            slider.Update ();
            caption_cover.Update ();
            caption_track.Update ();

            widget_bar.SetPosition (5, 5);
            RedrawViewport ();
        }

        //Update the coverStage position:
        protected void RedrawViewport ()
        {
            cover_manager.UpdateBehaviour ();
            cover_manager.SetRotation (RotateAxis.X, viewportAngleX, cover_manager.Width*0.5f, cover_manager.Height*0.5f,0);
            if (!cover_manager.IsVisible) {
                cover_manager.Show ();
            }
            cover_manager.LowerBottom ();
        }
        #endregion

        protected Cairo.ImageSurface GetDefaultSurface ()
        {
            Cairo.ImageSurface surface = new Cairo.ImageSurface(Cairo.Format.ARGB32, CoverManager.TextureSize, CoverManager.TextureSize);
            Cairo.Context context = new Cairo.Context(surface);
            Gdk.CairoHelper.SetSourcePixbuf(context, IconThemeUtils.LoadIcon (CoverManager.TextureSize, "media-optical", "browser-album-cover"), 0, 0);
            context.Paint();
            //((IDisposable) context.Target).Dispose();
            ((IDisposable) context).Dispose();
            return surface;
        }


        #region Event Handling

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            RedrawInterface ();
        }


        private void HandleAllocationChanged (object o, AllocationChangedArgs args)
        {
            RedrawInterface ();
        }


        private void HandleActorActivated (ClutterFlowBaseActor actor, EventArgs e)
        {
            var album_actor = actor as ClutterFlowAlbum;
            if (album_actor != null) {
                UpdateAlbum (album_actor);
            }
        }

        private void HandleButtonPressEvent (object o, Clutter.ButtonPressEventArgs args)
        {
            Clutter.EventHelper.GetCoords (args.Event, out drag_x0, out drag_y0);
            args.RetVal = true;
        }

        void HandleButtonReleaseEvent (object o, Clutter.ButtonReleaseEventArgs args)
        {
            //if (args.Event.Button==1 && !dragging  && coverManager.CurrentCover!=null && ActiveAlbum != CurrentAlbum)
            //    UpdateAlbum ();
            if (dragging) {
                Clutter.Ungrab.Pointer ();
            }
            dragging = false;
            args.RetVal = true;
        }

        private void HandleMotionEvent (object o, Clutter.MotionEventArgs args)
        {
            if ((args.Event.ModifierState.value__ & Clutter.ModifierType.Button1Mask.value__) != 0) {
                float drag_x; float drag_y;
                Clutter.EventHelper.GetCoords (args.Event, out drag_x, out drag_y);
                if (!dragging) {
                    if (Math.Abs(drag_x0 - drag_x) > 2 && Math.Abs(drag_y0 - drag_y) > 2) {
                        start_index = CoverManager.TargetIndex;
                        Clutter.Grab.Pointer (Stage);
                        dragging = true;
                    }
                } else {
                    if ((args.Event.ModifierState.value__ & Clutter.ModifierType.ControlMask.value__)!=0) {
                        if (!dragging) {
                            Clutter.Grab.Pointer (Stage);
                        }
                        ViewportAngleY += (float) (mouse_x - args.Event.X)*rotSens;
                        ViewportAngleX += (float) (mouse_y - args.Event.Y)*rotSens;
                    } else {
                        CoverManager.TargetIndex = start_index + (int) ((drag_x0 - drag_x)*drag_sens);
                    }
                }
            } else {
                if (dragging) {
                    Clutter.Ungrab.Pointer ();
                }
                dragging = false;
            }
            mouse_x = args.Event.X;
            mouse_y = args.Event.Y;

            args.RetVal = dragging;
        }

        private void HandleScroll (object o, Clutter.ScrollEventArgs args)
        {
            if (args.Event.Direction == Clutter.ScrollDirection.Down
                || args.Event.Direction == Clutter.ScrollDirection.Left) {
                Scroll (true);
            } else {
                Scroll (false);
            }
        }

        public void Scroll (bool Backward)
        {
            if (Backward) {
                cover_manager.TargetIndex--;
            } else {
                cover_manager.TargetIndex++;
            }
        }

        private void ScrollTo (string key)
        {
            ClutterFlowBaseActor actor = null;
            album_loader.Cache.TryGetValue (key, out actor);
            if (actor != null && cover_manager.Covers.Contains (actor)) {
                cover_manager.TargetIndex = actor.Index;
            }
        }

        public void ScrollTo (AlbumInfo album)
        {
            cover_manager.Timeline.Timeout = 500; //give 'm some time to load the song etc.
            ScrollTo (ClutterFlowAlbum.CreateCacheKey (album));
        }


        public void UpdateAlbum ()
        {
            UpdateAlbum (cover_manager.CurrentCover as ClutterFlowAlbum);
        }

        public void UpdateAlbum (ClutterFlowAlbum actor)
        {
            ActiveAlbum = actor.Album;
            ActiveIndex = actor.Index;
            if (UpdatedAlbum != null) {
                UpdatedAlbum (ActiveAlbum, EventArgs.Empty);
            }
        }

        protected void OnModelClearedHandler (object o, EventArgs args)
        {
            CoverManager.ReloadCovers ();
        }

        protected void OnModelReloadedHandler (object o, EventArgs args)
        {
            if (model_count != album_loader.Model.Count) {
                model_count = album_loader.Model.Count;
                CoverManager.ReloadCovers ();
            }
        }
        #endregion
    }
}
