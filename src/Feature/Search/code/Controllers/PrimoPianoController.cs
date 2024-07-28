using Sitecore.XA.Foundation.RenderingVariants.Controllers;
using UniCal.Feature.Search.Repositories;

namespace UniCal.Feature.Search.Controllers
{
    public class PrimoPianoController : VariantsController
    {
        private readonly IPrimoPianoRepository _primoPianoRepository;

        public PrimoPianoController(IPrimoPianoRepository primoPianoRepository)
        {
            _primoPianoRepository = primoPianoRepository;
        }

        protected override object GetModel() => (object)_primoPianoRepository.GetModel();
    }
}