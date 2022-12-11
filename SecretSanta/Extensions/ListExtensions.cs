using System.Collections;

namespace SecretSanta.Extensions;

internal static class ListExtensions
{
    public static void Shuffle(this IList list, int seed)
    {
        var random = new Random(seed);
        int iterations = list.Count * list.Count;
        for (int i = 0; i < iterations; i++)
        {
            int index1 = random.Next(list.Count);
            int index2 = random.Next(list.Count);

            (list[index1], list[index2]) = (list[index2], list[index1]);
        }
    }
}