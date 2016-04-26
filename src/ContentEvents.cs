using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace Epinova.Associations
{
    public class ContentEvents
    {
        public static void BindTwoWayRelationalContent(object sender, ContentEventArgs args)
        {
            var showstopper = ServiceLocator.Current.GetInstance<Showstopper>();
            if (showstopper.IsShowStoppedFor(args.Content.ContentLink.ID))
                return;

            var sourceRelationContent = args.Content as IHasTwoWayRelation;
            if (sourceRelationContent == null)
                return;

            var contentRepo = ServiceLocator.Current.GetInstance<IContentRepository>();

            var currentlyPublishedVersion = contentRepo.Get<IHasTwoWayRelation>(new ContentReference(args.Content.ContentLink.ID, true));

            var itemsToRemoveSourceFrom = GetItemsToRemoveSourceFrom(currentlyPublishedVersion, sourceRelationContent);

            foreach (var itemToRemoveSourceFrom in itemsToRemoveSourceFrom)
            {
                IHasTwoWayRelation relatedContentToRemoveFrom;
                if (!contentRepo.TryGet(itemToRemoveSourceFrom, out relatedContentToRemoveFrom))
                    continue;

                var writableContentToRemoveFrom = relatedContentToRemoveFrom.CreateWritableClone() as IHasTwoWayRelation;
                var contentAreaItem = writableContentToRemoveFrom.TwoWayRelatedContent.Items.FirstOrDefault(x => x.ContentLink.ID == sourceRelationContent.ContentLink.ID);
                writableContentToRemoveFrom.TwoWayRelatedContent.Items.Remove(contentAreaItem);

                showstopper.StopShowFor(writableContentToRemoveFrom.ContentLink.ID);

                contentRepo.Save(writableContentToRemoveFrom, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);
            }

            foreach (var item in sourceRelationContent.TwoWayRelatedContent.Items)
            {
                if (item.ContentLink.ID == sourceRelationContent.ContentLink.ID) // Avoid adding oneself, it'll only create trouble
                    continue;

                IHasTwoWayRelation relatedContent;
                if (!contentRepo.TryGet(item.ContentLink, out relatedContent))
                    continue;

                var alreadyContained = relatedContent.TwoWayRelatedContent != null &&
                                        relatedContent.TwoWayRelatedContent.Items.Any(x => x.ContentLink.ID == sourceRelationContent.ContentLink.ID);

                if (alreadyContained)
                    continue;

                showstopper.StopShowFor(item.ContentLink.ID);
                var writableRelatedContent = relatedContent.CreateWritableClone() as IHasTwoWayRelation;

                if (writableRelatedContent.TwoWayRelatedContent == null)
                    writableRelatedContent.TwoWayRelatedContent = new ContentArea();

                var newContentAreaItem = new ContentAreaItem { ContentLink = sourceRelationContent.ContentLink };
                writableRelatedContent.TwoWayRelatedContent.Items.Add(newContentAreaItem);

                contentRepo.Save(writableRelatedContent, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);
            }

            showstopper.StartShow();
        }

        private static IEnumerable<ContentReference> GetItemsToRemoveSourceFrom(IHasTwoWayRelation currentlyPublishedVersion, IHasTwoWayRelation sourceRelationContent)
        {
            var oldContentIds = currentlyPublishedVersion.TwoWayRelatedContent.Items.Select(x => x.ContentLink.ID);
            var newContentIds = sourceRelationContent.TwoWayRelatedContent.Items.Select(x => x.ContentLink.ID);
            var itemsToRemoveFrom = oldContentIds.Except(newContentIds);

            var itemsToRemoveSourceFrom = currentlyPublishedVersion.TwoWayRelatedContent.Items
                                                                                        .Where(x => itemsToRemoveFrom.Contains(x.ContentLink.ID))
                                                                                        .Select(x => x.ContentLink);

            return itemsToRemoveSourceFrom;
        }
    }
}