namespace BossMod.Endwalker.Alliance.A36Eulogia;

class TorrentialTridents : Components.RaidwideCastDelay
{
    public TorrentialTridents() : base(ActionID.MakeSpell(AID.TorrentialTridents), ActionID.MakeSpell(AID.Landing), 0.9f, "Raidwide x6") { }
}

class Tridents : Components.GenericAOEs
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeCircle _shape = new(18);
    public override IEnumerable<AOEInstance> ActiveAOEs(BossModule module, int slot, Actor actor)
    {
        if (_aoes.Count > 0)
            yield return new(_aoes[0].Shape, _aoes[0].Origin, _aoes[0].Rotation, _aoes[0].Activation, ArenaColor.Danger);
        if (_aoes.Count > 1)
            for (int i = 1; _aoes.Count == 6 ? i < _aoes.Count - 1 : i < _aoes.Count; ++i)
                yield return new(_aoes[i].Shape, _aoes[i].Origin, _aoes[i].Rotation, _aoes[i].Activation);
    }

    public override void OnEventCast(BossModule module, Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID is AID.Landing)
            _aoes.Add(new(_shape, caster.Position, activation: module.WorldState.CurrentTime.AddSeconds(13.1f)));
    }

    public override void OnCastFinished(BossModule module, Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.LightningBolt && _aoes.Count > 0)
            _aoes.RemoveAt(0);
    }
}