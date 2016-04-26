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
                IEnumerable<ContentReference> associationRemovalTargets = ContentAssociationsHelper.GetItemsToRemoveSourceFrom(property, currentlyPublishedVersion, associationSourceContent);

                foreach (var associationRemovalTarget in associationRemovalTargets)
                {
                    propertyWriter.RemoveAssociation(associationSourceContent, associationRemovalTarget, property);
                }

                IEnumerable<ContentReference> associationTargets = ContentAssociationsHelper.GetItemsToAddAssociationTo(property, associationSourceContent);

                foreach (var associationTarget in associationTargets)
                {
                    propertyWriter.AddAssociation(associationSourceContent, associationTarget, property);
                }
            }

            showstopper.StartShow();
        }

    }
}