using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Epinova.Associations;
using EPiServer.Core;
using NUnit.Framework;

namespace Epinova.AssociationsTests
{
    [TestFixture]
    public class ContentInspectorTests
    {
        [Test]
        public void GetAssociationProperties_ClassHasAssociationProperty_OnePropertyReturned()
        {
            var contentInspector = new ContentInspector();
            var associationProperties = contentInspector.GetAssociationProperties(new AssociationClass());
            Assert.AreEqual(1, associationProperties.Count());
        }

        [Test]
        public void GetAssociationProperties_ClassHasNoAssociationProperties_EmptyListReturned()
        {
            var contentInspector = new ContentInspector();
            var associationProperties = contentInspector.GetAssociationProperties(new AssociationLessClass());
            Assert.AreEqual(0, associationProperties.Count());
        }

        private class AssociationClass : PageData, IHasTwoWayRelation
        {
            [ContentAssociation]
            public virtual ContentArea ContentArea { get; set; }
        }

        private class AssociationLessClass : PageData, IHasTwoWayRelation
        {
        }
    }
}