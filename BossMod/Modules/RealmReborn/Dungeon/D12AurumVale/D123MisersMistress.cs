﻿namespace BossMod.RealmReborn.Dungeon.D12AurumVale.D123MisersMistress
{
    public enum OID : uint
    {
        Boss = 0x3AF, // x1
        MorbolFruit = 0x5BC, // spawn during fight
    };

    public enum AID : uint
    {
        AutoAttack = 1350, // Boss->player, no cast, single-target
        VineProbe = 1037, // Boss->self, 1.0s cast, range 6+R width 8 rect cleave (due to short cast time...)
        BadBreath = 1036, // Boss->self, 2.5s cast, range 12+R 120-degree cone aoe
        BurrBurrow = 1038, // Boss->self, 3.0s cast, raidwide?
        HookedBurrs = 1039, // Boss->player, 1.5s cast, single-target
        Sow = 1081, // Boss->player, 3.0s cast, single-target, spawns adds
    };

    class VineProbe : Components.Cleave
    {
        public VineProbe() : base(ActionID.MakeSpell(AID.VineProbe), new AOEShapeRect(10, 4)) { }
    }

    class BadBreath : Components.SelfTargetedAOEs
    {
        public BadBreath() : base(ActionID.MakeSpell(AID.BadBreath), new AOEShapeCone(16, 60.Degrees())) { }
    }

    // arena has multiple weirdly-shaped puddles, so just prefer standing in large safe zone
    class AIPosition : BossComponent
    {
        private AOEShapeRect _shape = new(5, 5, 5);
        private WPos[] _centers = { new(-395, -130), new(-402, -114) };

        public override void AddAIHints(BossModule module, int slot, Actor actor, AIHints hints)
        {
            hints.RestrictedZones.Add((_shape, _centers.MinBy(p => (p - module.PrimaryActor.Position).LengthSq()), new(), module.WorldState.CurrentTime));
        }
    }

    class D123MisersMistressStates : StateMachineBuilder
    {
        public D123MisersMistressStates(BossModule module) : base(module)
        {
            TrivialPhase()
                .ActivateOnEnter<VineProbe>()
                .ActivateOnEnter<BadBreath>()
                .ActivateOnEnter<AIPosition>();
        }
    }

    public class D123MisersMistress : BossModule
    {
        public D123MisersMistress(WorldState ws, Actor primary) : base(ws, primary, new ArenaBoundsSquare(new(-400, -130), 25)) { }

        public override bool FillTargets(BossTargets targets, int pcSlot)
        {
            if (!targets.AddIfValid(Enemies(OID.MorbolFruit)))
                targets.AddIfValid(PrimaryActor);
            return true;
        }
    }
}
