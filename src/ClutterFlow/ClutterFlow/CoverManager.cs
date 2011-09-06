//
// CoverManager.cs
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

using ClutterFlow.Alphabet;

using Clutter;
using Gtk;
using GLib;

namespace ClutterFlow
{
    public delegate void ActorEventHandler<T>(T actor, EventArgs e) where T : ClutterFlowBaseActor;

    public class CoverManager : Clutter.Group {

        #region Events
        public event ActorEventHandler<ClutterFlowBaseActor> ActorActivated;
        internal void InvokeActorActivated (ClutterFlowBaseActor cover)
        {
            var handler = ActorActivated;
            if (handler != null) {
                handler (cover, EventArgs.Empty);
            }
        }

        public event ActorEventHandler<ClutterFlowBaseActor> NewCurrentCover;
        protected void InvokeNewCurrentCover (ClutterFlowBaseActor cover)
        {
            var handler = NewCurrentCover;
            if (handler != null) {
                handler (cover, EventArgs.Empty);
            }
        }

        public event EventHandler<EventArgs> CoversChanged;
        protected void InvokeCoversChanged ()
        {
            var handler = CoversChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public event EventHandler<EventArgs> TargetIndexChanged;
        protected void InvokeTargetIndexChanged ()
        {
            var handler = TargetIndexChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public event EventHandler<EventArgs> VisibleCoversChanged;
        protected void InvokeVisibleCoversChanged ()
        {
            var handler = VisibleCoversChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public event EventHandler<EventArgs> LetterLookupChanged;
        protected void InvokeLetterLookupChanged ()
        {
            var handler = LetterLookupChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        #endregion

        #region Fields
        private static TextureHolder texture_holder;
        public static TextureHolder TextureHolder {
            get { return texture_holder; }
        }

        protected ClutterFlowTimeline timeline;
        public ClutterFlowTimeline Timeline {
            get {
                if (timeline == null) {
                    timeline = new ClutterFlowTimeline(this);
                    timeline.TargetMarkerReached += HandleTargetMarkerReached;
                }
                return timeline;
            }
        }

        protected IActorLoader actor_loader;    //Loads the actors (detaches ClutterFlow from Banshee related dependencies)
        public IActorLoader ActorLoader {
            get { return actor_loader; }
        }

        public Dictionary<AlphabetChars, int> letter_lookup;
        public Dictionary<AlphabetChars, int> LetterLookup {
            get { return letter_lookup; }
        }

        public void ResetLetterLookup () {
            letter_lookup = new Dictionary<AlphabetChars, int>();
            foreach (AlphabetChars key in Enum.GetValues(typeof(AlphabetChars)))
                letter_lookup.Add(key, -1);
        }
        public void UpdateLetterLookup (ClutterFlowBaseActor actor) {
            string label = actor.SortLabel.ToUpper ().Normalize (System.Text.NormalizationForm.FormKD);
            char letter = label.Length>0 ? char.Parse(label.Substring (0,1)) : '?';
            AlphabetChars key;
            if (char.IsLetter(letter))
                key = (AlphabetChars) letter;
            else
                key = AlphabetChars.unknown;
            if (letter_lookup.ContainsKey (key) && letter_lookup[key] == -1)
                letter_lookup[key] = actor.Index;
        }

        protected FlowBehaviour behaviour;
        public FlowBehaviour Behaviour {
            get { return behaviour; }
        }
        #endregion

        #region Cover-related fields

        protected int texture_size;
        public int TextureSize {
            get { return texture_size; }
            set { texture_size = value; }
        }

        protected int visibleCovers = 17;
        public int VisibleCovers {
            get { return visibleCovers; }
            set {
                if (value != visibleCovers) {
                    visibleCovers = value;
                    InvokeVisibleCoversChanged ();
                }
            }
        }
        public int HalfVisCovers {
            get { return (int) ((visibleCovers-1) * 0.5); }
        }
        #endregion

        #region Animation duration limits
        protected static uint maxAnimationSpan = 250; //maximal length of a single step in the animations in ms
        public static uint MaxAnimationSpan {
            get { return maxAnimationSpan; }
        }

        protected static uint minAnimationSpan = 10; //minimal length of a single step in the animations in ms
        public static uint MinAnimationSpan {
            get { return minAnimationSpan; }
        }

        protected static uint doubleClickTime = 200;
        public static uint DoubleClickTime {
            get { return doubleClickTime; }
            set { doubleClickTime = value; }
        }
        #endregion

        #region Target/Current index handling
        private int target_index = 0;            // curent targetted cover index
        public int TargetIndex {
            get { return target_index; }
            set {
                if (value >= TotalCovers) {
                    value = TotalCovers - 1;
                }
                if (value < 0) {
                    value = 0;
                }
                if (value != target_index) {
                    //Console.WriteLine ("TargetIndex_set to " + value);
                    target_index = value;
                    // To prevent clicks to load the old centered cover!
                    current_cover = null;
                    InvokeTargetIndexChanged();
                }
            }
        }

        public ClutterFlowBaseActor TargetActor {
            get {
                if (covers.Count > TargetIndex)
                    return covers[TargetIndex];
                else
                    return null;
            }
        }

        protected ClutterFlowFixedActor empty_actor;
        public ClutterFlowFixedActor EmptyActor {
            get {
                if (empty_actor == null)
                    empty_actor = new ClutterFlowFixedActor ((uint)behaviour.CoverWidth);
                return empty_actor;
            }
        }

        // list with cover actors
        private List<ClutterFlowBaseActor> covers;
        public List<ClutterFlowBaseActor> Covers {
            get { return covers; }
        }

        public int TotalCovers  {
            get { return (covers != null) ? covers.Count : 0; }
        }

        // currently centered cover
        private ClutterFlowBaseActor current_cover = null;
        public ClutterFlowBaseActor CurrentCover {
            get { return current_cover; }
            set {
                if (value != current_cover) {
                    current_cover = value;
                    InvokeNewCurrentCover (current_cover);
                }
            }
        }
        #endregion

        protected bool needs_reloading = false;
        public bool NeedsReloading {
            get { return needs_reloading; }
            internal set { needs_reloading = value; }
        }

        #region Initialisation
        public CoverManager (IActorLoader actor_loader, GetDefaultSurface get_default_surface, int texture_size) : base ()
        {
            this.actor_loader = actor_loader;
            this.texture_size = texture_size;
            behaviour = new FlowBehaviour (this);
            texture_holder = new TextureHolder (texture_size, get_default_surface);
        }

        public override void Dispose ()
        {
            if (reload_timeout > 0) {
                GLib.Source.Remove (reload_timeout);
            }

            behaviour.Dispose ();
            timeline.Dispose ();

            covers.Clear ();
            covers = null;
            current_cover = null;

            base.Dispose ();
        }
        #endregion

        /*new public ClutterFlowChildMeta GetChildMeta (Clutter.Actor actor)
        {
            return base.GetChildMeta(actor) as ClutterFlowChildMeta;
        }*/


        public void UpdateBehaviour ()
        {
            if (behaviour != null && Stage != null) {
                behaviour.Height = Stage.Height;
                behaviour.Width = Stage.Width;
                //Console.WriteLine ("behaviour.CoverWidth = " + behaviour.CoverWidth + "behaviour.Height = " + behaviour.Height + " behaviour.Width = " + behaviour.Width);
            }
        }

        void HandleTargetMarkerReached(object sender, TargetReachedEventArgs args)
        {
            if (args.Target==TargetIndex) {
                if (covers.Count > args.Target) {
                    CurrentCover = covers[(int) args.Target];
                } else {
                    CurrentCover = null;
                }
            }
        }

        private uint reload_timeout = 0;
        public void ReloadCovers ()
        {
            if (reload_timeout > 0) {
                GLib.Source.Remove(reload_timeout);
            }
            reload_timeout = GLib.Timeout.Add (MaxAnimationSpan, new GLib.TimeoutHandler (reload_covers));
        }

         private bool reload_covers ()
         {
            if (Timeline != null) {
                Timeline.Pause ();
            }
            HideAll ();
            Show ();
            if (covers != null && covers.Count != 0) {
                Console.WriteLine("ClutterFlow - Reloading Covers");

                // the old current index
                int old_target_index = CurrentCover!=null ? covers.IndexOf (CurrentCover) : 0;
                // the newly calculated index
                int new_target_index = 0;
                // whether or not to keep the current cover centered
                bool keep_current = false;

                List<ClutterFlowBaseActor> old_covers = new List<ClutterFlowBaseActor> (
                    SafeGetRange (covers, old_target_index - HalfVisCovers - 1, visibleCovers + 2));
                foreach (ClutterFlowBaseActor actor in covers) {
                    if (actor.Data.ContainsKey ("isOldCover")) {
                        actor.Data.Remove ("isOldCover");
                    }
                    actor.Index = -1;
                    if (old_covers.Contains (actor)) {
                        actor.Data.Add ("isOldCover", true);
                    }
                }

                ResetLetterLookup ();
                List<ClutterFlowBaseActor> persistent_covers = new List<ClutterFlowBaseActor>();

                covers = actor_loader.GetActors (this);
                covers.ForEach (delegate (ClutterFlowBaseActor actor) {
                    if (actor.Data.ContainsKey ("isOldCover")) {
                        persistent_covers.Add (actor);
                    }
                    if (CurrentCover==actor) {
                        keep_current = true;
                    }

                    UpdateLetterLookup (actor);
                });
                InvokeLetterLookupChanged ();

                if (covers.Count == 0) {
                    InstallEmptyActor ();
                    if (old_covers.Contains (EmptyActor)) {
                        EmptyActor.Show ();
                        return false;
                    }
                    keep_current = true;
                }

                //recalculate timeline progression and the target index
                if (covers.Count > 1) {
                    if (keep_current) {
                        new_target_index = CurrentCover.Index;
                    } else {
                        if (persistent_covers.Count==0) {
                            new_target_index = (int) Math.Round(Timeline.Progress * (covers.Count-1));
                        } else if (persistent_covers.Count==1) {
                            new_target_index = persistent_covers[0].Index;
                        } else {
                            new_target_index = persistent_covers[(int) (((float) persistent_covers.Count * 0.5f) - 1.0f)].Index;
                        }
                    }
                }
                TargetIndex = new_target_index;
                Timeline.JumpToTarget ();

                //Console.WriteLine ("Timeline progress set to " + Timeline.Progress + " Timeline.RelativeTarget is " + Timeline.RelativeTarget);

                List<ClutterFlowBaseActor> truly_pers = new List<ClutterFlowBaseActor> ();
                List<ClutterFlowBaseActor> new_covers = new List<ClutterFlowBaseActor>(SafeGetRange(covers, new_target_index - HalfVisCovers - 1, visibleCovers + 2));
                foreach (ClutterFlowBaseActor actor in persistent_covers) {
                    if (actor != null) {
                        if (actor.Data.ContainsKey ("isOldCover")) {
                            actor.Data.Remove ("isOldCover");
                        }
                        if (new_covers.Contains (actor)) {
                            truly_pers.Add (actor);
                            new_covers.Remove (actor);
                            old_covers.Remove (actor);
                            actor.Show ();
                        }
                    }
                }
                foreach (ClutterFlowBaseActor actor in old_covers) {
                    if (actor != null) {
                        actor.Data.Remove ("isOldCover");
                    }
                }

                /*Console.WriteLine ("old_covers          contains " + old_covers.Count + " elements:");
                foreach (ClutterFlowBaseActor cover in old_covers)
                    Console.WriteLine("\t- " + cover.Label.Replace("\n", " - "));
                Console.WriteLine ("persistent_covers   contains " + truly_pers.Count + " elements");
                foreach (ClutterFlowBaseActor cover in truly_pers)
                    Console.WriteLine("\t- " + cover.Label.Replace("\n", " - "));
                Console.WriteLine ("new_covers          contains " + new_covers.Count + " elements");
                foreach (ClutterFlowBaseActor cover in new_covers)
                    Console.WriteLine("\t- " + cover.Label.Replace("\n", " - "));*/

                EventHandler update_target = delegate (object o, EventArgs e) {
                    Timeline.Play ();
                    InvokeCoversChanged();
                };

                Behaviour.FadeCoversInAndOut (old_covers, truly_pers, new_covers, update_target);
            } else {
                Console.WriteLine("ClutterFlow - Loading Covers");
                ResetLetterLookup ();
                covers = actor_loader.GetActors (this);
                covers.ForEach (UpdateLetterLookup);
                InvokeLetterLookupChanged ();
                TargetIndex = 0;
                Timeline.JumpToTarget ();
                if (covers==null || covers.Count==0) {
                    InstallEmptyActor ();
                    Behaviour.UpdateActors ();
                    Behaviour.FadeInActor (EmptyActor);
                }
                Timeline.Play ();
                InvokeCoversChanged ();
            }

            return false;
        }

        private void InstallEmptyActor ()
        {
            covers = new List<ClutterFlowBaseActor> ();
            covers.Add (EmptyActor);
            CurrentCover = EmptyActor;
        }

        private IEnumerable<ClutterFlowBaseActor> SafeGetRange (List<ClutterFlowBaseActor> list, int index, int count) {
            for (int i = index; i < index + count; i++) {
                ClutterFlowBaseActor cover = null;
                if (i >= 0 && i < list.Count) {
                    cover = list[i];
                }
                if (cover == null) {
                    continue;
                }
                yield return cover;
            }
            yield break;
        }

        public void ForSomeCovers (System.Action<ClutterFlowBaseActor> method_call, int lBound, int uBound) {
            if (covers != null && lBound <= uBound && lBound >= 0 && uBound < covers.Count) {
                IEnumerator<ClutterFlowBaseActor> enumerator = covers.GetRange(lBound, uBound-lBound + 1).GetEnumerator();
                while (enumerator.MoveNext()) {
                    method_call(enumerator.Current);
                }
            }
        }
    }
}
