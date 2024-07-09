using Sitecore.XA.Foundation.IoC;
using Sitecore.XA.Foundation.Mvc.Repositories.Base;

namespace UniCal.Feature.Composites.Repositories
{
    public interface IPrimoPianoRepository : IModelRepository, IControllerRepository, IAbstractRepository<IRenderingModelBase>
    {
    }
}
