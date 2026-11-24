using UnityEngine;

public static class RunRewards
{
    public const int SeasonDays = 5;     // 1 star: lasted a season
    public const int FullRunDays = 20;   // 3 stars: finished the whole Guided story

    public static int Stars(MyRuns r)
    {
        if (r.days_survived < SeasonDays) return 0;

        int stars = 1;
        if (r.outcome == "BothSurvived" && r.endsheep >= 2 && r.endleopard >= 2) stars++;
        if (r.days_survived >= FullRunDays) stars++;
        return stars;
    }
}
