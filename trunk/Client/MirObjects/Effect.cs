﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Client.MirGraphics;
using Client.MirScenes;
using Client.MirSounds;

namespace Client.MirObjects
{
    public class Effect
    {
        public MLibrary Library;

        public int BaseIndex, Count, Duration;
        public long Start;

        public int CurrentFrame;
        public long NextFrame;

        public Point Source;
        public MapObject Owner;

        public int Light = 6;
        public bool Blend = true;
        public float Rate = 1F;
        public Point DrawLocation;
        public bool Repeat;
        public long RepeatUntil;
        
        public event EventHandler Complete;

        public Effect(MLibrary library, int baseIndex, int count, int duration, MapObject owner, long starttime = 0)
        {
            Library = library;
            BaseIndex = baseIndex;
            Count = count == 0 ? 1 : count;
            Duration = duration;
            Start = starttime == 0 ? CMain.Time : starttime;

            NextFrame = Start + (Duration / Count) * (CurrentFrame + 1);
            Owner = owner;
            Source = Owner.CurrentLocation;
        }
        public Effect(MLibrary library, int baseIndex, int count, int duration, Point source, long starttime = 0)
        {
            Library = library;
            BaseIndex = baseIndex;
            Count = count == 0 ? 1 : count;
            Duration = duration;
            Start = starttime == 0 ? CMain.Time : starttime;

            NextFrame = Start + (Duration / Count) * (CurrentFrame + 1);
            Source = source;
        }

        public void SetStart(long start)
        {
            Start = start;

            NextFrame = Start + (Duration / Count) * (CurrentFrame + 1);
        }

        public virtual void Process()
        {
            if (CMain.Time <= NextFrame) return;

            if (Owner != null && Owner.SkipFrames) CurrentFrame++;

            if (++CurrentFrame >= Count)
            {
                if (Repeat && (RepeatUntil == 0 || CMain.Time < RepeatUntil))
                {
                    CurrentFrame = 0;
                    Start = CMain.Time;
                    NextFrame = Start + (Duration / Count) * (CurrentFrame + 1);
                }
                else
                    Remove();
            }
            else NextFrame = Start + (Duration / Count) * (CurrentFrame + 1);

            GameScene.Scene.MapControl.TextureValid = false;


        }
        public virtual void Remove()
        {
            if (Owner != null)
                Owner.Effects.Remove(this);
            else
                MapControl.Effects.Remove(this);

            if (Complete != null)
                Complete(this, EventArgs.Empty);
        }

        public virtual void Draw()
        {
            if (CMain.Time < Start) return;

            if (Owner != null)
            {
                DrawLocation = Owner.DrawLocation;
            }
            else
            {
                DrawLocation = new Point((Source.X - MapObject.User.Movement.X + MapControl.OffSetX) * MapControl.CellWidth,
                                         (Source.Y - MapObject.User.Movement.Y + MapControl.OffSetY) * MapControl.CellHeight);
                DrawLocation.Offset(MapObject.User.OffSetMove);
            }


            if (Blend)
                Library.DrawBlend(BaseIndex + CurrentFrame, DrawLocation, Color.White, true, Rate);
            else
                Library.Draw(BaseIndex + CurrentFrame, DrawLocation, Color.White, true);
        }

        public void Clear()
        {
            Complete = null;
        }
    }

    public class Missile : Effect
    {
        public static List<Missile> Missiles = new List<Missile>();
        public MapObject Target;
        public Point Destination;
        public int Interval, FrameCount, Skip;
        public int Direction;
        public bool Explode;


        public Missile(MLibrary library, int baseIndex, int count, int duration, MapObject owner, Point target, bool direction16 = true)
            : base(library, baseIndex, count, duration, owner)
        {
            Missiles.Add(this);
            Source = Owner.CurrentLocation;
            Destination = target;
            Direction = direction16 ? MapControl.Direction16(Source, Destination) : (int)Functions.DirectionFromPoint(Source, Destination);
        }

        public Missile(MLibrary library, int baseIndex, int count, int duration, Point source, Point target)
            : base(library, baseIndex, count, duration, source)
        {
            Missiles.Add(this);
            Destination = target;

            Direction = MapControl.Direction16(Source, Destination);
        }

        public override void Process()
        {
            if (CMain.Time < Start) return;

            if (Target != null) Destination = Target.CurrentLocation;
            else if (!Explode)
            {
                int dist = Functions.MaxDistance(Owner.CurrentLocation, Destination);

                if (dist < Globals.DataRange)
                    Destination.Offset(Destination.X - Source.X, Destination.Y - Source.Y);
            }

            Duration = Functions.MaxDistance(Source, Destination) * 50;
            Count = Duration / Interval;
            if (Count == 0) Count = 1;

            base.Process();
        }
        public override void Remove()
        {
            base.Remove();
            Missiles.Remove(this);
        }
        public override void Draw()
        {
            if (CMain.Time < Start) return;


            int index = BaseIndex + (CurrentFrame % FrameCount) + Direction * (Skip + FrameCount);

            DrawLocation = new Point((Source.X - MapObject.User.Movement.X + MapControl.OffSetX) * MapControl.CellWidth,
                                       (Source.Y - MapObject.User.Movement.Y + MapControl.OffSetY) * MapControl.CellHeight);
            DrawLocation.Offset(MapObject.User.OffSetMove);

            int x = (Destination.X - Source.X) * MapControl.CellWidth;
            int y = (Destination.Y - Source.Y) * MapControl.CellHeight;


            DrawLocation.Offset(x * CurrentFrame / Count, y * CurrentFrame / Count);

            if (!Blend)
                Library.Draw(index, DrawLocation, Color.White, true);
            else
                Library.DrawBlend(index, DrawLocation, Color.White, true, Rate);
        }

    }

    public class InterruptionEffect : Effect//ArcherSpells - Elemental system
    {
        public static List<InterruptionEffect> effectlist = new List<InterruptionEffect>();
        bool noProcess = false;

        public InterruptionEffect(MLibrary library, int baseIndex, int count, int duration, MapObject owner, bool blend, long starttime = 0)
            : base(library, baseIndex, count, duration, owner)
        {
            Repeat = true;
            Blend = blend;
            effectlist.Add(this);
        }

        public override void Process()
        {
            if (!noProcess)
                base.Process();
        }

        public override void Remove()
        {
            base.Remove();
            effectlist.Remove(this);
        }

        public override void Draw()
        {
            if (!((PlayerObject)Owner).Concentrating)
                Remove();
            else if (((PlayerObject)Owner).ConcentrateInterrupted)
                noProcess = true;
            else
                noProcess = false;
            if (!noProcess)
                base.Draw();
        }

        public static int GetOwnerEffectID(uint objectID)
        {
            for (int i = 0; i < effectlist.Count; i++)
            {
                if (effectlist[i].Owner.ObjectID != objectID) continue;
                return i;
            }
            return -1;
        }
    }

    public class ElementsEffect : Effect//ArcherSpells - Elemental system
    {
        int myType;//1 = green orb, 2 = blue orb, 3 = red orb, 4 = mixed orbs
        long killAt;//holds the exp value for 4 orbs : kills all orbs when myType 4 is reached
        bool loopit = false;//soundloop

        public ElementsEffect(MLibrary library, int baseIndex, int count, int duration, MapObject owner, bool blend, int elementType, int killtime, bool loopon = false)
            : base(library, baseIndex, count, duration, owner)
        {
            Repeat = true;
            Blend = blend;
            myType = elementType;
            killAt = killtime;
            //
            loopit = loopon;
            StopSounds();
            StartSound();
        }

        public override void Process()
        {

            base.Process();
        }

        private void StartSound()
        {
            SoundManager.PlaySound(20000 + 126 * 10 + 4 + myType, loopit);
        }

        private void StopSounds()
        {
            for (int i = 0; i <= 3; i++)
                SoundManager.StopSound(20000 + 126 * 10 + 5 + i);
        }

        public override void Remove()
        {
            SoundManager.StopSound(20000 + 126 * 10 + 4 + myType);
            base.Remove();
        }

        public override void Draw()
        {
            if (!((PlayerObject)Owner).HasElements)
                Remove();
            if (((PlayerObject)Owner).ElementsLevel >= killAt && myType < 4)
                Remove();
            base.Draw();
        }
    }
}
