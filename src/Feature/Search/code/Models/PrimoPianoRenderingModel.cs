using Sitecore.XA.Foundation.Variants.Abstractions.Models;
using System.Linq;
using UniCal.Foundation.Search.Models;

namespace UniCal.Feature.Search.Models
{
    public class PrimoPianoRenderingModel : VariantsRenderingModel
    {
        public IQueryable<DocumentModel> Documents { get; set; }
    }
}