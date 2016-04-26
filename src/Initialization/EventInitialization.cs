using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace Epinova.Associations.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class EventInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            IContentEvents events = ServiceLocator.Current.GetInstance<IContentEvents>();
            events.PublishingContent += ContentEvents.BindTwoWayRelationalContent;
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}