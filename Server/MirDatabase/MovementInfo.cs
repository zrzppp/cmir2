﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Server.MirEnvir;

namespace Server.MirDatabase
{
    public class MovementInfo
    {
        public int MapIndex;
        public Point Source, Destination;
        public bool NeedHole;

        public MovementInfo()
        {

        }

        public MovementInfo(BinaryReader reader)
        {
            MapIndex = reader.ReadInt32();
            Source = new Point(reader.ReadInt32(), reader.ReadInt32());
            Destination = new Point(reader.ReadInt32(), reader.ReadInt32());

            if (Envir.LoadVersion < 16) return;
            NeedHole = reader.ReadBoolean();
        }
        public void Save(BinaryWriter writer)
        {
            writer.Write(MapIndex);
            writer.Write(Source.X);
            writer.Write(Source.Y);
            writer.Write(Destination.X);
            writer.Write(Destination.Y);
            writer.Write(NeedHole);
        }


        public override string ToString()
        {
            return string.Format("{0} -> Map :{1} - {2}", Source, MapIndex, Destination);
        }
    }
}
