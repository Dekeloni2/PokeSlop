using System.Collections.Generic;
using System.Linq;
using FinalProject.Events;

namespace FinalProject.Core
{
    // which way the run is going. sparing everyone is the good ending, killing
    // everyone is the other one, and anything in between is neutral
    public enum Route { Pacifist, Neutral, Genocide }

    // remembers how every teacher's fight ended, so the ending can react to the
    // whole run rather than the last fight.
    //
    // this is the one thing in the game that legitimately wants the event bus:
    // it needs the outcome of every fight, present and future, and no battle
    // should have to know it exists. a teacher is identified by name, so adding
    // one stays a JSON file with no code change here.
    public class RouteTracker
    {
        private readonly Dictionary<string, BattleOutcome> _resolved = new();

        // lives as long as the game does, so there's no matching Unsubscribe.
        // starting a fresh run calls Reset instead of building a new tracker
        public RouteTracker()
            => EventBus.Instance.Subscribe<TeacherResolvedEvent>(OnTeacherResolved);

        // refighting a teacher overwrites the old result — the last way you left
        // him is the one that counts
        private void OnTeacherResolved(TeacherResolvedEvent e)
            => _resolved[e.TeacherId] = e.Outcome;

        public int Killed => _resolved.Values.Count(o => o == BattleOutcome.Killed);
        public int Spared => _resolved.Values.Count(o => o == BattleOutcome.Spared);

        // which teachers specifically, not just how many. the ending picks its
        // script by the exact set of who was killed, so "spared Yakir, killed
        // David" is a different ending from the other way round
        public IEnumerable<string> KilledIds
            => _resolved.Where(p => p.Value == BattleOutcome.Killed).Select(p => p.Key);

        public IEnumerable<string> SparedIds
            => _resolved.Where(p => p.Value == BattleOutcome.Spared).Select(p => p.Key);

        // how many fights have been settled either way
        public int ResolvedCount => _resolved.Count;

        // killing is what taints a run, so a run with no kills is still clean
        // even before anyone has been fought
        public Route Current => Killed == 0 ? Route.Pacifist
                              : Spared == 0 ? Route.Genocide
                              :               Route.Neutral;

        // null when that teacher hasn't been fought to a finish yet
        public BattleOutcome? OutcomeFor(string teacherId)
            => _resolved.TryGetValue(teacherId, out BattleOutcome outcome) ? outcome : null;

        public bool IsResolved(string teacherId) => _resolved.ContainsKey(teacherId);

        // whether a teacher's prerequisites are all settled. either way a fight
        // ended counts — this asks "have you faced them", not "how did it go".
        // no prerequisites means there's nothing to wait for
        public bool HasResolvedAll(IEnumerable<string> teacherIds)
            => teacherIds == null || teacherIds.All(IsResolved);

        public void Reset() => _resolved.Clear();
    }
}
