using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            var associationSourceContent = args.Content as IHasTwoWayRelation;
            if (associationSourceContent == null)
                return;

            var contentRepo = ServiceLocator.Current.GetInstance<IContentRepository>();
            var propertyWriter = ServiceLocator.Current.GetInstance<PropertyWriter>();

            var currentlyPublishedVersion = contentRepo.Get<IHasTwoWayRelation>(new ContentReference(args.Content.ContentLink.ID, true));

            var associationProperties = ContentAssociationsHelper.GetAssociationProperties(associationSourceContent);

            foreach (var property in associationProperties)
            {
                IEnumerable<ContentReference> itemsToRemoveSourceFrom = ContentAssociationsHelper.GetItemsToRemoveSourceFrom(property, currentlyPublishedVersion, associationSourceContent);

                foreach (var itemToRemoveSourceFrom in itemsToRemoveSourceFrom)
                {
                    IHasTwoWayRelation relatedContentToRemoveFrom;
                    if (!contentRepo.TryGet(itemToRemoveSourceFrom, out relatedContentToRemoveFrom))
                        continue;

                    var writableContentToRemoveFrom = relatedContentToRemoveFrom.CreateWritableClone() as IHasTwoWayRelation;
                    var propertyToRemoveFrom = writableContentToRemoveFrom.GetType().GetProperties().FirstOrDefault(x => x.Name == property.Name);

                    if (propertyToRemoveFrom.PropertyType == typeof (ContentArea))
                    {
                        ContentArea contentArea = propertyToRemoveFrom.GetValue(writableContentToRemoveFrom) as ContentArea;
                        if (contentArea == null)
                            continue;

                        var itemToRemove = contentArea.Items.FirstOrDefault(x => x.ContentLink.ID == associationSourceContent.ContentLink.ID);
                        if (itemToRemove != null)
                            contentArea.Items.Remove(itemToRemove);
                    }

                    if (propertyToRemoveFrom.PropertyType == typeof (IList<ContentReference>))
                    {
                        IList<ContentReference> contentRefList = propertyToRemoveFrom.GetValue(writableContentToRemoveFrom) as IList<ContentReference>;
                        var itemToRemove = contentRefList.FirstOrDefault(x => x.ID == associationSourceContent.ContentLink.ID);
                        contentRefList.Remove(itemToRemove);
                    }

                    showstopper.StopShowFor(writableContentToRemoveFrom.ContentLink.ID);
                    contentRepo.Save(writableContentToRemoveFrom, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);
                }

                IEnumerable<ContentReference> associationTargets = ContentAssociationsHelper.GetItemsToAddAssociationTo(property, associationSourceContent);

                foreach (var associationTarget in associationTargets)
                {
                    propertyWriter.AddAssociation(associationTarget, associationSourceContent, property);
                }
            }

            showstopper.StartShow();
        }
    }
}