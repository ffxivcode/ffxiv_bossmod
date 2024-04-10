namespace BossMod.AI;

[ConfigDisplay(Name = "AI settings (very experimental!!!)", Order = 6)]
class AIConfig : ConfigNode
{
    [PropertyDisplay("Enable AI")]
    public bool Enabled = false;

    [PropertyDisplay("Show Status in DTR Bar")]
    public bool ShowDTR = true;

    [PropertyDisplay("Draw UI")]
    public bool DrawUI = true;

    [PropertyDisplay("Follow Leader")]
    public bool FollowLeader = true;

    [PropertyDisplay("Focus Target Leader")]
    public bool FocusTargetLeader = true;

    [PropertyDisplay("Broadcast keypresses to other windows")]
    public bool BroadcastToSlaves = false;
}
