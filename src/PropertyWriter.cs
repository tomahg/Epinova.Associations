using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;

namespace Epinova.Associations
{
    internal class PropertyWriter
    {
        private readonly IContentRepository _contentRepository;
        private readonly Showstopper _showstopper;

        public PropertyWriter(IContentRepository contentRepository, Showstopper showstopper)
        {
            _contentRepository = contentRepository;
            _showstopper = showstopper;
        }

        public void AddAssociation(IHasTwoWayRelation associationSource, ContentReference associationTarget, PropertyInfo property)
        {
            if (associationTarget.ID == associationSource.ContentLink.ID) // Avoid adding oneself, it'll only create trouble
                return;

            IHasTwoWayRelation relatedContent;
            if (!_contentRepository.TryGet(associationTarget, out relatedContent))
                return;

            var relatedPropertyContent = relatedContent.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(x => x.Name == property.Name);

            var writableRelatedContent = relatedContent.CreateWritableClone() as IHasTwoWayRelation;
            if (relatedPropertyContent.PropertyType == typeof(ContentArea))
            {
                var relatedContentArea = relatedPropertyContent.GetValue(writableRelatedContent) as ContentArea;

                var alreadyContained = relatedContentArea != null &&
                                       relatedContentArea.Items.Any(x => x.ContentLink.ID == associationSource.ContentLink.ID);

                if (alreadyContained)
                    return;

                if (relatedContentArea == null)
                {
                    relatedContentArea = new ContentArea();
                    relatedPropertyContent.SetValue(writableRelatedContent, relatedContentArea);
                }

                var newContentAreaItem = new ContentAreaItem { ContentLink = associationSource.ContentLink };
                relatedContentArea.Items.Add(newContentAreaItem);
            }

            if (relatedPropertyContent.PropertyType == typeof(IList<ContentReference>))
            {
                var relatedContentRefList = relatedPropertyContent.GetValue(writableRelatedContent) as IList<ContentReference>;

                var alreadyContained = relatedContentRefList != null &&
                                       relatedContentRefList.Any(x => x.ID == associationSource.ContentLink.ID);

                if (alreadyContained)
                    return;

                if (relatedContentRefList == null)
                {
                    relatedContentRefList = new List<ContentReference>();
                    relatedPropertyContent.SetValue(writableRelatedContent, relatedContentRefList);
                }

                relatedContentRefList.Add(associationSource.ContentLink);
            }

            _showstopper.StopShowFor(associationTarget.ID);
            _contentRepository.Save(writableRelatedContent, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);
        }


        public void RemoveAssociation(IHasTwoWayRelation associationSourceContent, ContentReference associationRemovalTarget, PropertyInfo property)
        {
            IHasTwoWayRelation relatedContentToRemoveFrom;
            if (!_contentRepository.TryGet(associationRemovalTarget, out relatedContentToRemoveFrom))
                return;

            var writableContentToRemoveFrom = relatedContentToRemoveFrom.CreateWritableClone() as IHasTwoWayRelation;
            var propertyToRemoveFrom = writableContentToRemoveFrom.GetType().GetProperties().FirstOrDefault(x => x.Name == property.Name);

            if (propertyToRemoveFrom.PropertyType == typeof(ContentArea))
            {
                ContentArea contentArea = propertyToRemoveFrom.GetValue(writableContentToRemoveFrom) as ContentArea;
                if (contentArea == null)
                    return;

                var itemToRemove = contentArea.Items.FirstOrDefault(x => x.ContentLink.ID == associationSourceContent.ContentLink.ID);
                if (itemToRemove != null)
                    contentArea.Items.Remove(itemToRemove);
            }

            if (propertyToRemoveFrom.PropertyType == typeof(IList<ContentReference>))
            {
                IList<ContentReference> contentRefList = propertyToRemoveFrom.GetValue(writableContentToRemoveFrom) as IList<ContentReference>;
                if (contentRefList == null)
                    return;

                var itemToRemove = contentRefList.FirstOrDefault(x => x.ID == associationSourceContent.ContentLink.ID);
                contentRefList.Remove(itemToRemove);
            }

            _showstopper.StopShowFor(writableContentToRemoveFrom.ContentLink.ID);
            _contentRepository.Save(writableContentToRemoveFrom, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);
        }

    }
}