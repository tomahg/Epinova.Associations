using System.Collections.Generic;
using EPiServer;
using EPiServer.Core;
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
            var contentAssociationsHelper = ServiceLocator.Current.GetInstance<ContentAssociationsHelper>();

            var currentContentVersion = contentRepo.Get<IHasTwoWayRelation>(new ContentReference(args.Content.ContentLink.ID, true));

            var associationProperties = contentAssociationsHelper.GetAssociationProperties(associationSourceContent);

            foreach (var property in associationProperties)
            {
                IEnumerable<ContentReference> associationRemovalTargets = contentAssociationsHelper.GetAssociationRemovalTargets(property, currentContentVersion, associationSourceContent);

                foreach (var associationRemovalTarget in associationRemovalTargets)
                    propertyWriter.RemoveAssociation(associationSourceContent, associationRemovalTarget, property);
                
                IEnumerable<ContentReference> associationTargets = contentAssociationsHelper.GetAssociationTargets(property, associationSourceContent);

                foreach (var associationTarget in associationTargets)
                    propertyWriter.AddAssociation(associationSourceContent, associationTarget, property);
            }

            showstopper.StartShow();
        }
    }
}