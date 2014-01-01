﻿using System;
using System.Drawing;
using Server.MirDatabase;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    class Yimoogi : MonsterObject
    {
        protected byte AttackRange = 7;
        protected byte PoisonAttackRange = 4;
        public bool NoAttack, ChildSpawned, IsChild, FinalTeleport;
        protected Yimoogi MasterMob, ChildMob;
        public long SpawnTime;
        public int WhiteSnakeCount = 2;

        protected internal Yimoogi(MonsterInfo info)
            : base(info)
        {
            SpawnTime = Envir.Time + 4000;
        }

        protected bool ChildMobReady()
        {
            if (ChildMob == null) return false;
            if (ChildMob.Dead) return false;

            return true;
        }

        protected bool MasterMobReady()
        {
            
            if (MasterMob == null) return false;
            if (MasterMob.Dead) return false;

            return true;
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 2 || y > 2) return false;

            return (x <= 1 && y <= 1) || (x == y || x % 2 == y % 2);
        }

        protected bool InRangedAttackRange(int distance)
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, distance);
        }

        protected override void FindTarget()
        {
            if (!NoAttack) base.FindTarget();
        }

        protected override void Attack()
        {

            if (!Target.IsAttackTarget(this) || NoAttack)
            {
                Target = null;
                return;
            }

            ShockTime = 0;

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            bool ranged = CurrentLocation == Target.CurrentLocation || !InAttackRange();

            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;

            int damage = GetAttackPower(MinDC, MaxDC);
            if (!ranged && Envir.Random.Next(5) > 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
                if (damage == 0) return;

                Target.Attacked(this, damage, DefenceType.MACAgility);
            }
            else
            {
                AttackTime = Envir.Time + AttackSpeed + 500;
                if (damage == 0) return;

                if (InRangedAttackRange(PoisonAttackRange) && Envir.Random.Next(6) == 0)
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });

                    Target.ApplyPoison(new Poison
                    {
                        Owner = this,
                        Duration = 6,
                        PType = PoisonType.Red,
                        TickSpeed = 2000
                    });                
                }
                else
                {
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID });

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, Target, damage, DefenceType.MAC);
                    ActionList.Add(action);
                }
            }


            if (Target.Dead)
                FindTarget();

        }

        public override void Process()
        {
            base.Process();

            if (Target == null) NoAttack = true;
        }

        protected override void ProcessAI()
        {
            if (Dead) return;

            if (!IsChild && HP <= MaxHP / 10 && !FinalTeleport)
            {
                Point teleportlocation = CurrentLocation;
                if (TeleportRandom(40, 0))
                {
                    FinalTeleport = true;
         
                    MonsterObject mob = null;
                    for (int i = 0; i < WhiteSnakeCount; i++)
                    {
                        mob = GetMonster(Envir.GetMonsterInfo(Settings.WhiteSnake));
                        if (mob == null) continue;

                        if (!mob.Spawn(CurrentMap, teleportlocation)) continue;

                        mob.Target = Target;
                        mob.ActionTime = Envir.Time + 2000;
                    }
                    Target = null;
                }
            }

            if (!IsChild && !ChildSpawned && Envir.Time > SpawnTime)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                SpawnSlave();
                return;
            }

            if (IsChild && MasterMobReady())
            {
                if (Functions.InRange(CurrentLocation, MasterMob.CurrentLocation, 2) && Target == null && 
                Functions.MaxDistance(CurrentLocation, MasterMob.CurrentLocation) < 10)
                {
                    MoveTo(MasterMob.CurrentLocation);
                    return;
                }
            }

            base.ProcessAI();
        }

        protected override void ProcessTarget()
        {
            if (Target == null) return;
            if (InAttackRange() && !CanAttack) return;

            NoAttack = false;

            if (!IsChild && ChildMobReady())
            {
                if (ChildMob.Target == null) ChildMob.Target = Target;
            }
            if (IsChild && MasterMobReady())
            {
                if (MasterMob.Target == null) MasterMob.Target = Target;
            }

            if (InRangedAttackRange(AttackRange) && CanAttack)
            {
                Attack();
                return;
            }

            if (Envir.Time < ShockTime)
            {
                Target = null;
                return;
            }

            MoveTo(Target.CurrentLocation);
        }

        protected void SpawnSlave()
        {
            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;

            MonsterObject mob = GetMonster(Envir.GetMonsterInfo(Name));

            if (mob == null) base.ProcessAI();
            Yimoogi childmob = (Yimoogi)mob;

            if (!childmob.Spawn(CurrentMap, Front))
               childmob.Spawn(CurrentMap, CurrentLocation);

            if (Target != null)
            {
                childmob.Target = Target;
                childmob.NoAttack = false;
            }

            childmob.IsChild = true;
            childmob.MasterMob = this;
            childmob.ActionTime = Envir.Time + 2000;
            ChildMob = childmob;
            ChildSpawned = true;
        }

        public override bool TeleportRandom(int attempts, int distance)
        {
            for (int i = 0; i < attempts; i++)
            {
                Point location;

                if (distance <= 0)
                    location = new Point(Envir.Random.Next(CurrentMap.Width), Envir.Random.Next(CurrentMap.Height));
                else
                    location = new Point(CurrentLocation.X + Envir.Random.Next(-distance, distance + 1),
                                         CurrentLocation.Y + Envir.Random.Next(-distance, distance + 1));

                if (Teleport(CurrentMap, location, true, 1)) return true;
            }

            return false;
        }

        protected override void Drop()
        {
            if (!ChildMobReady() && !MasterMobReady())
                base.Drop();
        }
    }
}
