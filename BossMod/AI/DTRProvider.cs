using Dalamud.Game.Gui.Dtr;

namespace BossMod.AI;

class DTRProvider() : IDisposable
{
    private DtrBarEntry _dtrBarEntry = Service.DtrBar.Get("Bossmod");

    public void Dispose()
    {
        _dtrBarEntry.Dispose();
    }

    public void Update(AIBehaviour? behaviour)
    {
        _dtrBarEntry.Shown = Service.Config.Get<AIConfig>().ShowDTR;
        if (_dtrBarEntry.Shown)
        {
            var status = behaviour != null ? "On" : "Off";
            _dtrBarEntry.Text = "AI: " + status;
        }
    }
}
