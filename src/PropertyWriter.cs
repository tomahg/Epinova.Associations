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

            IHasTwoWayRelation associatedContent;
            if (!_contentRepository.TryGet(associationTarget, out associatedContent))
                return;

            var associationTargetProperty = associatedContent.GetType()
                                                             .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                             .FirstOrDefault(x => x.Name == property.Name);

            var writableRelatedContent = associatedContent.CreateWritableClone() as IHasTwoWayRelation;
            if (associationTargetProperty.PropertyType == typeof(ContentArea))
            {
                var relatedContentArea = associationTargetProperty.GetValue(writableRelatedContent) as ContentArea;
                if (IsAlreadyContained(associationSource, relatedContentArea))
                    return;

                if (relatedContentArea == null)
                {
                    relatedContentArea = new ContentArea();
                    associationTargetProperty.SetValue(writableRelatedContent, relatedContentArea);
                }

                var newContentAreaItem = new ContentAreaItem { ContentLink = associationSource.ContentLink };
                relatedContentArea.Items.Add(newContentAreaItem);
            }

            if (associationTargetProperty.PropertyType == typeof(IList<ContentReference>))
            {
                var associationTargetContentRefList = associationTargetProperty.GetValue(writableRelatedContent) as IList<ContentReference>;
                if (IsAlreadyContained(associationSource, associationTargetContentRefList))
                    return;

                if (associationTargetContentRefList == null)
                {
                    associationTargetContentRefList = new List<ContentReference>();
                    associationTargetProperty.SetValue(writableRelatedContent, associationTargetContentRefList);
                }

                associationTargetContentRefList.Add(associationSource.ContentLink);
            }

            _showstopper.StopShowFor(associationTarget.ID);
            _contentRepository.Save(writableRelatedContent, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);
        }
        
        public void RemoveAssociation(IHasTwoWayRelation associationSourceContent, ContentReference associationRemovalTarget, PropertyInfo property)
        {
            IHasTwoWayRelation associationRemovalTargetContent;
            if (!_contentRepository.TryGet(associationRemovalTarget, out associationRemovalTargetContent))
                return;

            var writableAssociationRemovalTargetContent = associationRemovalTargetContent.CreateWritableClone() as IHasTwoWayRelation;
            var associationRemovalTargetProperty = writableAssociationRemovalTargetContent.GetType().GetProperties().FirstOrDefault(x => x.Name == property.Name);

            if (associationRemovalTargetProperty.PropertyType == typeof(ContentArea))
            {
                ContentArea contentArea = associationRemovalTargetProperty.GetValue(writableAssociationRemovalTargetContent) as ContentArea;
                if (contentArea == null)
                    return;

                var itemToRemove = contentArea.Items.FirstOrDefault(x => x.ContentLink.ID == associationSourceContent.ContentLink.ID);
                if (itemToRemove != null)
                    contentArea.Items.Remove(itemToRemove);
            }

            if (associationRemovalTargetProperty.PropertyType == typeof(IList<ContentReference>))
            {
                IList<ContentReference> contentRefList = associationRemovalTargetProperty.GetValue(writableAssociationRemovalTargetContent) as IList<ContentReference>;
                if (contentRefList == null)
                    return;

                var itemToRemove = contentRefList.FirstOrDefault(x => x.ID == associationSourceContent.ContentLink.ID);
                contentRefList.Remove(itemToRemove);
            }

            _showstopper.StopShowFor(writableAssociationRemovalTargetContent.ContentLink.ID);
            _contentRepository.Save(writableAssociationRemovalTargetContent, SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);
        }

        private bool IsAlreadyContained(IHasTwoWayRelation associationSource, ContentArea associationTargetContentArea)
        {
            return associationTargetContentArea != null &&
                   associationTargetContentArea.Items.Any(x => x.ContentLink.ID == associationSource.ContentLink.ID);
        }

        private bool IsAlreadyContained(IHasTwoWayRelation associationSource, IList<ContentReference> associationTargetContentRefList)
        {
            return associationTargetContentRefList != null &&
                   associationTargetContentRefList.Any(x => x.ID == associationSource.ContentLink.ID);
        }
    }
}