using System.Collections.Generic;

namespace Epinova.Associations
{
    internal class Showstopper
    {
        private List<int> ContentIdsToIgnoreEvents { get; set; }

        public Showstopper()
        {
            ContentIdsToIgnoreEvents = new List<int>();
        }

        public void StopShowFor(int contentId)
        {
            if (ContentIdsToIgnoreEvents.Contains(contentId))
                return;
            ContentIdsToIgnoreEvents.Add(contentId);
        }

        public bool IsShowStoppedFor(int contentId)
        {
            return ContentIdsToIgnoreEvents.Contains(contentId);
        }

        public void StartShow()
        {
            ContentIdsToIgnoreEvents.RemoveAll(x => true);
        }
    }
}