using System.Collections.Generic;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace Epinova.Associations
{
    internal class ContentEvents
    {
        public static void BindTwoWayRelationalContent(object sender, ContentEventArgs args)
        {
            //can't really do anything without a contentlink in the args...
            if (ContentReference.IsNullOrEmpty(args.ContentLink))
                return;

            var showstopper = ServiceLocator.Current.GetInstance<Showstopper>();
            if (showstopper.IsShowStoppedFor(args.Content.ContentLink.ID))
                return;

            var associationSourceContent = args.Content as IAssociationContent;
            if (associationSourceContent == null)
                return;

            var contentRepo = ServiceLocator.Current.GetInstance<IContentRepository>();
            var propertyWriter = ServiceLocator.Current.GetInstance<PropertyWriter>();
            var contentAssociationsHelper = ServiceLocator.Current.GetInstance<ContentInspector>();

            var currentContentVersion = contentRepo.Get<IAssociationContent>(new ContentReference(args.Content.ContentLink.ID, 0, args.Content.ContentLink.ProviderName, true));

            var associationProperties = contentAssociationsHelper.GetAssociationProperties(associationSourceContent);

            foreach (var property in associationProperties)
            {
                IEnumerable<ContentReference> associationRemovalTargets = contentAssociationsHelper.GetAssociationRemovalTargets(property, currentContentVersion, associationSourceContent);

                var propertyName = property.Name;
                foreach (var associationRemovalTarget in associationRemovalTargets)
                    propertyWriter.RemoveAssociation(associationSourceContent, associationRemovalTarget, propertyName);
                
                IEnumerable<ContentReference> associationTargets = contentAssociationsHelper.GetAssociationTargets(property, associationSourceContent);

                foreach (var associationTarget in associationTargets)
                    propertyWriter.AddAssociation(associationSourceContent, associationTarget, propertyName);
            }

            showstopper.StartShow();
        }
    }
}