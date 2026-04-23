using UnityEngine;

public static class TimeParser
{
    public static int ToMinutes(string time)
    {
        if (string.IsNullOrEmpty(time))
        {
            Debug.LogError("Pusty czas!");
            return 0;
        }

        time = time.Trim();
        time = time.Replace('.', ':').Replace('-', ':');

        string[] parts = time.Split(':');

        if (parts.Length != 2)
        {
            Debug.LogError("Z³y format czasu: " + time);
            return 0;
        }

        int h = int.Parse(parts[0]);
        int m = int.Parse(parts[1]);

        if (h == 24) h = 0; // obs³uga 24:00

        return h * 60 + m;
    }

    public static int CurrentMinutes()
    {
        var now = System.DateTime.Now;
        return now.Hour * 60 + now.Minute;
    }
}