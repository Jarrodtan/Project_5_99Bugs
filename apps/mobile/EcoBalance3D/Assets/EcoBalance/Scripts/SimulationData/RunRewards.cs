using UnityEngine;

public static class RunRewards
{
    // 1 to 3 stars, each easy to explain to a child.
    public static int Stars(MyRuns r)
    {
        int stars = 0;
        if (r.outcome == "BothSurvived") stars++;          // both animals still alive at the end
        if (r.endtrees > 0) stars++;                        // the trees survived too

        var before = RunHist.previous();                    // the last saved run (this one isn't saved yet)
        if (before != null && r.days_survived > before.days_survived) stars++;   // beat the last run

        return stars;
    }
}
