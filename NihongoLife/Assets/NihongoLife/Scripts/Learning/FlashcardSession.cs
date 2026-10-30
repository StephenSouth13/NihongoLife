using System.Collections.Generic;

namespace NihongoLife.Learning
{
    /// <summary>One Anki-style review sitting at the study desk: due cards in order, flip, grade (0 Again..3 Easy).
    /// Pure rules; FlashcardScreen draws it. "Again" cards go back to the end of the queue for this sitting.</summary>
    public sealed class FlashcardSession
    {
        public readonly struct Face { public readonly string front, back; public Face(string f, string b) { front = f; back = b; } }

        private readonly Queue<FlashcardState> _queue;
        private readonly IDictionary<string, Face> _faces;
        private readonly int _today;
        public int Reviewed { get; private set; }
        public int Again { get; private set; }
        public bool Flipped { get; private set; }
        public FlashcardState Current => _queue.Count > 0 ? _queue.Peek() : null;
        public bool Done => _queue.Count == 0;
        public int Remaining => _queue.Count;

        public FlashcardSession(IEnumerable<FlashcardState> cards, IDictionary<string, Face> faces, int today, int max = 20)
        {
            _faces = faces ?? new Dictionary<string, Face>();
            _today = today;
            _queue = new Queue<FlashcardState>(FlashcardScheduler.Due(cards, today, max));
        }

        public string Front => Current != null && _faces.TryGetValue(Current.id, out var f) ? f.front : Current?.id ?? "";
        public string Back => Current != null && _faces.TryGetValue(Current.id, out var f) ? f.back : "";

        public void Flip() { if (!Done) Flipped = true; }

        /// <summary>Grade the shown card. Ignored until the card is flipped (no grading blind).</summary>
        public bool Grade(int grade)
        {
            if (Done || !Flipped) return false;
            var c = _queue.Dequeue();
            FlashcardScheduler.Review(c, grade, _today);
            Reviewed++;
            if (grade <= 0) { Again++; _queue.Enqueue(c); }
            Flipped = false;
            return true;
        }
    }
}
