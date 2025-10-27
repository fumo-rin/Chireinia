using UnityEngine;

namespace FumoCore.Tools
{
    public partial class Helper
    {
        static int[] randomIntTable;
        static int randomIntIndex;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void FillTable()
        {
            randomIntIndex = 0;
            int length = 256;
            randomIntTable = new int[length];
            int seed = 3378;
            System.Random r = new System.Random(seed);
            for (int i = 0; i < length; i++)
            {
                randomIntTable[i] = r.Next(0, length);
            }
        }
        public static int SeededRandomInt256 => GetRandomInt();
        static int GetRandomInt()
        {
            if (randomIntTable == null)
            {
                FillTable();
            }
            if (randomIntIndex >= randomIntTable.Length)
            {
                randomIntIndex = 0;
            }
            return randomIntTable[randomIntIndex++];
        }
        public static int RandomSign()
        {
            return Random.value < 0.5f ? -1 : 1;
        }
    }
}
