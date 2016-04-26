using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
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

            var associationProperties = GetAssociationProperties(sourceRelationContent);

            foreach (var property in associationProperties)
            {
                IEnumerable<ContentReference> itemsToRemoveSourceFrom = GetItemsToRemoveSourceFrom(property, currentlyPublishedVersion, sourceRelationContent);

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
                        var itemToRemove = contentArea.Items.FirstOrDefault(x => x.ContentLink.ID == sourceRelationContent.ContentLink.ID);
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

                if (property.PropertyType == typeof (ContentArea))
                {
                    var contentArea = property.GetValue(sourceRelationContent) as ContentArea;

                    foreach (var item in contentArea.Items)
                    {
                        if (item.ContentLink.ID == sourceRelationContent.ContentLink.ID) // Avoid adding oneself, it'll only create trouble
                            continue;

                        IHasTwoWayRelation relatedContent;
                        if (!contentRepo.TryGet(item.ContentLink, out relatedContent))
                            continue;

                        var relatedPropertyContent = relatedContent.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(x => x.Name == property.Name);

                        var writableRelatedContent = relatedContent.CreateWritableClone() as IHasTwoWayRelation;
                        if (relatedPropertyContent.PropertyType == typeof (ContentArea))
                        {
                            var relatedContentArea = relatedPropertyContent.GetValue(writableRelatedContent) as ContentArea;

                            var alreadyContained = relatedContentArea != null &&
                                                   relatedContentArea.Items.Any(x => x.ContentLink.ID == sourceRelationContent.ContentLink.ID);

                            if (alreadyContained)
                                continue;
                            
                            if (relatedContentArea == null)
                                relatedContentArea = new ContentArea();

                            var newContentAreaItem = new ContentAreaItem { ContentLink = sourceRelationContent.ContentLink };
                            relatedContentArea.Items.Add(newContentAreaItem);
                        }
                        if (relatedPropertyContent.PropertyType == typeof (IList<ContentReference>))
                        {
                            var relatedContentRefList = relatedPropertyContent.GetValue(writableRelatedContent) as IList<ContentReference>;

                            var alreadyContained = relatedContentRefList != null &&
                                                   relatedContentRefList.Any(x => x.ID == sourceRelationContent.ContentLink.ID);

                            if (alreadyContained)
                                continue;

                            if (relatedContentRefList == null)
                                relatedContentRefList = new List<ContentReference>();

                            relatedContentRefList.Add(sourceRelationContent.ContentLink);
                        }

                        showstopper.StopShowFor(item.ContentLink.ID);
                        contentRepo.Save(writableRelatedContent, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);
                    }
                }
            }

            showstopper.StartShow();
        }

        private static IEnumerable<ContentReference> GetItemsToRemoveSourceFrom(PropertyInfo property, IHasTwoWayRelation currentlyPublishedVersion, IHasTwoWayRelation sourceRelationContent)
        {
            if (property.PropertyType == typeof (ContentArea))
            {
                var currentContent = property.GetValue(currentlyPublishedVersion) as ContentArea;
                var newContent = property.GetValue(sourceRelationContent) as ContentArea;

                var oldContentIds = currentContent.Items.Select(x => x.ContentLink.ID);
                var newContentIds = newContent.Items.Select(x => x.ContentLink.ID);

                var itemsToRemoveFrom = oldContentIds.Except(newContentIds);

                return  currentContent.Items
                                      .Where(x => itemsToRemoveFrom.Contains(x.ContentLink.ID))
                                      .Select(x => x.ContentLink);
            }

            if (property.PropertyType == typeof (IList<ContentReference>))
            {
                var currentContent = property.GetValue(currentlyPublishedVersion) as IList<ContentReference>;
                var newContent = property.GetValue(sourceRelationContent) as IList<ContentReference>;

                var oldContentIds = currentContent.Select(x => x.ID);
                var newContentIds = newContent.Select(x => x.ID);

                var itemsToRemoveFrom = oldContentIds.Except(newContentIds);

                return currentContent.Where(x => itemsToRemoveFrom.Contains(x.ID));
            }

            throw new Exception("Attempt to use property on unsupported property. Currently, ContentArea and IList<ContentArea> is supported");
        }

        private static IEnumerable<PropertyInfo> GetAssociationProperties(IHasTwoWayRelation sourceRelationContent)
        {
            var contentType = sourceRelationContent.GetType();

            var contentProperties = contentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in contentProperties.Where(x => x.PropertyType == typeof(ContentArea) ||
                                                                  x.PropertyType == typeof(IList<ContentReference>)))
            {
                var associationAttribute = property.GetCustomAttributes(typeof(ContentAssociationAttribute)).FirstOrDefault() as ContentAssociationAttribute;
                if (associationAttribute == null)
                    continue;

                yield return property;
            }
        }

        //private static IEnumerable<ContentReference> GetItemsToRemoveSourceFrom(IHasTwoWayRelation currentlyPublishedVersion, IHasTwoWayRelation sourceRelationContent)
        //{
        //    var oldContentIds = currentlyPublishedVersion.TwoWayRelatedContent.Items.Select(x => x.ContentLink.ID);
        //    var newContentIds = sourceRelationContent.TwoWayRelatedContent.Items.Select(x => x.ContentLink.ID);
        //    var itemsToRemoveFrom = oldContentIds.Except(newContentIds);

        //    var itemsToRemoveSourceFrom = currentlyPublishedVersion.TwoWayRelatedContent.Items
        //                                                                                .Where(x => itemsToRemoveFrom.Contains(x.ContentLink.ID))
        //                                                                                .Select(x => x.ContentLink);

        //    return itemsToRemoveSourceFrom;
        //}
    }
}