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

            var sourceRelationContent = args.Content as IHasTwoWayRelation;
            if (sourceRelationContent == null)
                return;

            var contentRepo = ServiceLocator.Current.GetInstance<IContentRepository>();
            var currentlyPublishedVersion = contentRepo.Get<IHasTwoWayRelation>(new ContentReference(args.Content.ContentLink.ID, true));

            var associationProperties = ContentAssociationsHelper.GetAssociationProperties(sourceRelationContent);

            foreach (var property in associationProperties)
            {
                IEnumerable<ContentReference> itemsToRemoveSourceFrom = ContentAssociationsHelper.GetItemsToRemoveSourceFrom(property, currentlyPublishedVersion, sourceRelationContent);

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

                        var itemToRemove = contentArea.Items.FirstOrDefault(x => x.ContentLink.ID == sourceRelationContent.ContentLink.ID);
                        if (itemToRemove != null)
                            contentArea.Items.Remove(itemToRemove);
                    }

                    if (propertyToRemoveFrom.PropertyType == typeof (IList<ContentReference>))
                    {
                        IList<ContentReference> contentRefList = propertyToRemoveFrom.GetValue(writableContentToRemoveFrom) as IList<ContentReference>;
                        var itemToRemove = contentRefList.FirstOrDefault(x => x.ID == sourceRelationContent.ContentLink.ID);
                        contentRefList.Remove(itemToRemove);
                    }

                    showstopper.StopShowFor(writableContentToRemoveFrom.ContentLink.ID);
                    contentRepo.Save(writableContentToRemoveFrom, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);
                }

                IEnumerable<ContentReference> itemsToAddAssociationTo = ContentAssociationsHelper.GetItemsToAddAssociationTo(property, sourceRelationContent);

                foreach (var item in itemsToAddAssociationTo)
                {
                    if (item.ID == sourceRelationContent.ContentLink.ID) // Avoid adding oneself, it'll only create trouble
                        continue;

                    IHasTwoWayRelation relatedContent;
                    if (!contentRepo.TryGet(item, out relatedContent))
                        continue;

                    var relatedPropertyContent = relatedContent.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(x => x.Name == property.Name);

                    var writableRelatedContent = relatedContent.CreateWritableClone() as IHasTwoWayRelation;
                    if (relatedPropertyContent.PropertyType == typeof(ContentArea))
                    {
                        var relatedContentArea = relatedPropertyContent.GetValue(writableRelatedContent) as ContentArea;

                        var alreadyContained = relatedContentArea != null &&
                                               relatedContentArea.Items.Any(x => x.ContentLink.ID == sourceRelationContent.ContentLink.ID);

                        if (alreadyContained)
                            continue;

                        if (relatedContentArea == null) { 
                            relatedContentArea = new ContentArea();
                            relatedPropertyContent.SetValue(writableRelatedContent, relatedContentArea);
                        }

                        var newContentAreaItem = new ContentAreaItem { ContentLink = sourceRelationContent.ContentLink };
                        relatedContentArea.Items.Add(newContentAreaItem);
                    }

                    if (relatedPropertyContent.PropertyType == typeof(IList<ContentReference>))
                    {
                        var relatedContentRefList = relatedPropertyContent.GetValue(writableRelatedContent) as IList<ContentReference>;

                        var alreadyContained = relatedContentRefList != null &&
                                               relatedContentRefList.Any(x => x.ID == sourceRelationContent.ContentLink.ID);

                        if (alreadyContained)
                            continue;

                        if (relatedContentRefList == null)
                        {
                            relatedContentRefList = new List<ContentReference>();
                            relatedPropertyContent.SetValue(writableRelatedContent, relatedContentRefList);
                        }

                        relatedContentRefList.Add(sourceRelationContent.ContentLink);
                    }

                    showstopper.StopShowFor(item.ID);
                    contentRepo.Save(writableRelatedContent, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);
                }
            }

            showstopper.StartShow();
        }
    }
}