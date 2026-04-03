namespace Utils;

public static class ListExtensions
{
    public static void Shuffle<T>(this List<T> list)
    {
        var rng = Random.Shared;

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}