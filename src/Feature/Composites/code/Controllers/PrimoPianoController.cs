using Sitecore.XA.Foundation.Mvc.Controllers;
using UniCal.Feature.Composites.Repositories;

namespace UniCal.Feature.Composites.Controllers
{
    public class PrimoPianoController : StandardController
    {
        private readonly IPrimoPianoRepository _primoPianoRepository;

        public PrimoPianoController(IPrimoPianoRepository primoPianoRepository)
        {
            _primoPianoRepository = primoPianoRepository;
        }

        protected override object GetModel() => (object) _primoPianoRepository.GetModel();
    }
}