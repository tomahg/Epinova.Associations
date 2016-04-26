using System.Collections.Generic;

namespace Epinova.Associations
{
    /// <summary>
    /// This class exists to make sure Episerver doesn't go wild and save everything recursively when a lot of relations have been added.
    /// </summary>
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