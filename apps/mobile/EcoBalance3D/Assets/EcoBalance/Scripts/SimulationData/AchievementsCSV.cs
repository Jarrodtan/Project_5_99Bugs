using UnityEngine;
using TMPro;

public class AchievementsCSV : MonoBehaviour
{
    public DataLogging Logging;
    public TextMeshPro Titletxt;
    public TextMeshPro Detailstxt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenSummary();
    }

    public void GenSummary()
    {
        if (Logging != null) {
            Debug.LogWarning("DataLogger is missing!");
            return;
        }

        int total_days = Logging.GetTotalTicks();
        int sheep_amt = Logging.GetPeakBS();
        int leopard_amt = Logging.GetPeakSL();

        if (Titletxt != null)
        {
            Titletxt.text = "EcoBalance Adventure";
        }

        if (Detailstxt != null) {
            Detailstxt.text = $"• Days Explored: <b>{total_days}</b>\n" +
                $"• Peak Blue Sheep Herd: <b>{sheep_amt}</b> 🐑\n" +
                $"• Peak Snow Leopards: <b>{leopard_amt}</b> 🐆\n\n" +
                "<i>Great job on exploring the mountains! Check your Field Journal to see what cases you unlocked!</i>";
        }

    }
}
