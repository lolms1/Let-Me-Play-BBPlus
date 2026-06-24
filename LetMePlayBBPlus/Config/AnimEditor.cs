namespace LetMePlayBBPlus
{
    public static class AnimEditor
    {
        private static Dictionary<string, AnimSequence> sequences = new Dictionary<string, AnimSequence>();

        public static void SetSequences(Dictionary<string, AnimSequence> data)
        {
            sequences = data ?? new Dictionary<string, AnimSequence>();
        }
        public static AnimSequence GetSequence(string audioKey)
        {
            sequences.TryGetValue(audioKey, out AnimSequence seq);
            return seq;
        }
    }
}
